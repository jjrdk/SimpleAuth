﻿#region copyright
// Copyright 2015 Habart Thierry
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using SimpleBus.Core;
using SimpleIdentityServer.Core;
using SimpleIdentityServer.Core.Common.DTOs;
using SimpleIdentityServer.Core.Translation;
using SimpleIdentityServer.Core.WebSite.Consent;
using SimpleIdentityServer.Core.WebSite.User;
using SimpleIdentityServer.EventStore.Core.Models;
using SimpleIdentityServer.EventStore.Core.Repositories;
using SimpleIdentityServer.Handler.Events;
using SimpleIdentityServer.Host.Extensions;
using SimpleIdentityServer.Host.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Host.Controllers.Website
{
    [Authorize("Connected")]
    public class ConsentController : BaseController
    {
        private readonly IConsentActions _consentActions;
        private readonly IDataProtector _dataProtector;
        private readonly ITranslationManager _translationManager;
        private readonly IEventPublisher _eventPublisher;
        private readonly IEventAggregateRepository _eventAggregateRepository;
        private readonly IPayloadSerializer _payloadSerializer;

        public ConsentController(
            IConsentActions consentActions,
            IDataProtectionProvider dataProtectionProvider,
            ITranslationManager translationManager,
            IEventPublisher eventPublisher,
            IEventAggregateRepository eventAggregateRepository,
            IAuthenticationService authenticationService,
            IUserActions usersAction,
            IPayloadSerializer payloadSerializer,
            AuthenticateOptions authenticateOptions) : base(authenticationService, authenticateOptions)
        {
            _consentActions = consentActions;
            _dataProtector = dataProtectionProvider.CreateProtector("Request");
            _translationManager = translationManager;
            _eventPublisher = eventPublisher;
            _eventAggregateRepository = eventAggregateRepository;
            _payloadSerializer = payloadSerializer;
        }
        
        public async Task<ActionResult> Index(string code)
        {
            var request = _dataProtector.Unprotect<AuthorizationRequest>(code);
            var client = new Core.Common.Models.Client();
            var authenticatedUser = await SetUser();
            var actionResult = await _consentActions.DisplayConsent(request.ToParameter(),
                authenticatedUser);

            var result = this.CreateRedirectionFromActionResult(actionResult.ActionResult, request);
            if (result != null)
            {
                return result;
            }

            await TranslateConsentScreen(request.UiLocales);
            var viewModel = new ConsentViewModel
            {
                ClientDisplayName = client.ClientName,
                AllowedScopeDescriptions = actionResult.Scopes == null ? new List<string>() : actionResult.Scopes.Select(s => s.Description).ToList(),
                AllowedIndividualClaims = actionResult.AllowedClaims == null ? new List<string>() : actionResult.AllowedClaims,
                LogoUri = client.LogoUri,
                PolicyUri = client.PolicyUri,
                TosUri = client.TosUri,
                Code = code
            };
            return View(viewModel);
        }
        
        public async Task<ActionResult> Confirm(string code)
        {
            var request = _dataProtector.Unprotect<AuthorizationRequest>(code);
            var parameter = request.ToParameter();
            var authenticatedUser = await _authenticationService.GetAuthenticatedUser(this, _authenticateOptions.CookieName);
            var actionResult = await _consentActions.ConfirmConsent(parameter,
                authenticatedUser);
            await LogConsentAccepted(actionResult, parameter.ProcessId);
            return this.CreateRedirectionFromActionResult(actionResult,
                request);
        }

        /// <summary>
        /// Action executed when the user refuse the consent.
        /// It redirects to the callback without passing the authorization code in parameter.
        /// </summary>
        /// <param name="code">Encrypted & signed authorization request</param>
        /// <returns>Redirect to the callback url.</returns>
        public async Task<ActionResult> Cancel(string code)
        {
            var request = _dataProtector.Unprotect<AuthorizationRequest>(code);
            await LogConsentRejected(request.ProcessId);
            return Redirect(request.RedirectUri);
        }

        private async Task TranslateConsentScreen(string uiLocales)
        {
            // Retrieve the translation and store them in a ViewBag
            var translations = await _translationManager.GetTranslationsAsync(uiLocales, new List<string>
            {
                Core.Constants.StandardTranslationCodes.ApplicationWouldLikeToCode,
                Core.Constants.StandardTranslationCodes.IndividualClaimsCode,
                Core.Constants.StandardTranslationCodes.ScopesCode,
                Core.Constants.StandardTranslationCodes.CancelCode,
                Core.Constants.StandardTranslationCodes.ConfirmCode,
                Core.Constants.StandardTranslationCodes.LinkToThePolicy,
                Core.Constants.StandardTranslationCodes.Tos
            });
            ViewBag.Translations = translations;
        }

        private async Task LogConsentAccepted(Core.Results.ActionResult act, string processId)
        {
            if (string.IsNullOrWhiteSpace(processId))
            {
                return;
            }

            var evtAggregate = await GetLastEventAggregate(processId);
            if (evtAggregate == null)
            {
                return;
            }

            _eventPublisher.Publish(new ConsentAccepted(Guid.NewGuid().ToString(), processId, _payloadSerializer.GetPayload(act), evtAggregate.Order + 1));
        }

        private async Task LogConsentRejected(string processId)
        {
            if (string.IsNullOrWhiteSpace(processId))
            {
                return;
            }

            var evtAggregate = await GetLastEventAggregate(processId);
            if (evtAggregate == null)
            {
                return;
            }

            _eventPublisher.Publish(new ConsentRejected(Guid.NewGuid().ToString(), processId, evtAggregate.Order + 1));
        }

        private async Task<EventAggregate> GetLastEventAggregate(string aggregateId)
        {
            var events = (await _eventAggregateRepository.GetByAggregate(aggregateId)).OrderByDescending(e => e.Order);
            if (events == null || !events.Any())
            {
                return null;
            }

            return events.First();
        }
    }
}