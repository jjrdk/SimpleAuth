﻿// Copyright © 2015 Habart Thierry, © 2018 Jacob Reimers
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

namespace SimpleAuth.WebSite.Consent.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Api.Authorization;
    using Common;
    using Errors;
    using Exceptions;
    using Extensions;
    using Factories;
    using Helpers;
    using Logging;
    using Parameters;
    using Results;
    using Shared.Models;
    using Shared.Repositories;

    public class ConfirmConsentAction : IConfirmConsentAction
    {
        private readonly IConsentRepository _consentRepository;
        private readonly IClientStore _clientRepository;
        private readonly IScopeRepository _scopeRepository;
        private readonly IResourceOwnerRepository _resourceOwnerRepository;
        private readonly IParameterParserHelper _parameterParserHelper;
        private readonly IActionResultFactory _actionResultFactory;
        private readonly IGenerateAuthorizationResponse _generateAuthorizationResponse;
        private readonly IConsentHelper _consentHelper;
        private readonly IOpenIdEventSource _openidEventSource;

        public ConfirmConsentAction(
            IConsentRepository consentRepository,
            IClientStore clientRepository,
            IScopeRepository scopeRepository,
            IResourceOwnerRepository resourceOwnerRepository,
            IParameterParserHelper parameterParserHelper,
            IActionResultFactory actionResultFactory,
            IGenerateAuthorizationResponse generateAuthorizationResponse,
            IConsentHelper consentHelper,
            IOpenIdEventSource openidEventSource)
        {
            _consentRepository = consentRepository;
            _clientRepository = clientRepository;
            _scopeRepository = scopeRepository;
            _resourceOwnerRepository = resourceOwnerRepository;
            _parameterParserHelper = parameterParserHelper;
            _actionResultFactory = actionResultFactory;
            _generateAuthorizationResponse = generateAuthorizationResponse;
            _consentHelper = consentHelper;
            _openidEventSource = openidEventSource;
        }

        /// <summary>
        /// This method is executed when the user confirm the consent
        /// 1). If there's already consent confirmed in the past by the resource owner
        /// 1).* then we generate an authorization code and redirects to the callback.
        /// 2). If there's no consent then we insert it and the authorization code is returned
        ///  2°.* to the callback url.
        /// </summary>
        /// <param name="authorizationParameter">Authorization code grant-type</param>
        /// <param name="claimsPrincipal">Resource owner's claims</param>
        /// <returns>Redirects the authorization code to the callback.</returns>
        public async Task<EndpointResult> Execute(
            AuthorizationParameter authorizationParameter,
            ClaimsPrincipal claimsPrincipal, string issuerName)
        {
            if (authorizationParameter == null)
            {
                throw new ArgumentNullException(nameof(authorizationParameter));
            }

            if (claimsPrincipal?.Identity == null)
            {
                throw new ArgumentNullException(nameof(claimsPrincipal));
            }

            var client = await _clientRepository.GetById(authorizationParameter.ClientId).ConfigureAwait(false);
            if (client == null)
            {
                throw new InvalidOperationException($"the client id {authorizationParameter.ClientId} doesn't exist");
            }

            var subject = claimsPrincipal.GetSubject();
            var assignedConsent = await _consentHelper.GetConfirmedConsentsAsync(subject, authorizationParameter).ConfigureAwait(false);
            // Insert a new consent.
            if (assignedConsent == null)
            {
                var claimsParameter = authorizationParameter.Claims;
                if (claimsParameter.IsAnyIdentityTokenClaimParameter() ||
                    claimsParameter.IsAnyUserInfoClaimParameter())
                {
                    // A consent can be given to a set of claims
                    assignedConsent = new Consent
                    {
                        Id = Guid.NewGuid().ToString(),
                        Client = client,
                        ResourceOwner = await _resourceOwnerRepository.Get(subject).ConfigureAwait(false),
                        Claims = claimsParameter.GetClaimNames()
                    };
                }
                else
                {
                    // A consent can be given to a set of scopes
                    assignedConsent = new Consent
                    {
                        Id = Guid.NewGuid().ToString(),
                        Client = client,
                        GrantedScopes = (await GetScopes(authorizationParameter.Scope).ConfigureAwait(false)).ToList(),
                        ResourceOwner = await _resourceOwnerRepository.Get(subject).ConfigureAwait(false),
                    };
                }

                // A consent can be given to a set of claims
                await _consentRepository.InsertAsync(assignedConsent).ConfigureAwait(false);

                _openidEventSource.GiveConsent(subject,
                    authorizationParameter.ClientId,
                    assignedConsent.Id);
            }

            var result = _actionResultFactory.CreateAnEmptyActionResultWithRedirectionToCallBackUrl();
            await _generateAuthorizationResponse.ExecuteAsync(result, authorizationParameter, claimsPrincipal, client, issuerName).ConfigureAwait(false);

            // If redirect to the callback and the responde mode has not been set.
            if (result.Type == TypeActionResult.RedirectToCallBackUrl)
            {
                var responseMode = authorizationParameter.ResponseMode;
                if (responseMode == ResponseMode.None)
                {
                    var responseTypes = _parameterParserHelper.ParseResponseTypes(authorizationParameter.ResponseType);
                    var authorizationFlow = GetAuthorizationFlow(responseTypes, authorizationParameter.State);
                    responseMode = GetResponseMode(authorizationFlow);
                }

                result.RedirectInstruction.ResponseMode = responseMode;
            }

            return result;
        }
        
        /// <summary>
        /// Returns a list of scopes from a concatenate list of scopes separated by whitespaces.
        /// </summary>
        /// <param name="concatenateListOfScopes"></param>
        /// <returns>List of scopes</returns>
        private async Task<ICollection<Scope>> GetScopes(string concatenateListOfScopes)
        {
            var result = new List<Scope>();
            var scopeNames = _parameterParserHelper.ParseScopes(concatenateListOfScopes);
            return await _scopeRepository.SearchByNames(scopeNames).ConfigureAwait(false);
        }

        private static AuthorizationFlow GetAuthorizationFlow(ICollection<ResponseType> responseTypes, string state)
        {
            if (responseTypes == null)
            {
                throw new SimpleAuthExceptionWithState(
                    ErrorCodes.InvalidRequestCode,
                    ErrorDescriptions.TheAuthorizationFlowIsNotSupported,
                    state);
            }

            var record = CoreConstants.MappingResponseTypesToAuthorizationFlows.Keys
                .SingleOrDefault(k => k.Count == responseTypes.Count && k.All(key => responseTypes.Contains(key)));
            if (record == null)
            {
                throw new SimpleAuthExceptionWithState(
                    ErrorCodes.InvalidRequestCode,
                    ErrorDescriptions.TheAuthorizationFlowIsNotSupported,
                    state);
            }

            return CoreConstants.MappingResponseTypesToAuthorizationFlows[record];
        }

        private static ResponseMode GetResponseMode(AuthorizationFlow authorizationFlow)
        {
            return CoreConstants.MappingAuthorizationFlowAndResponseModes[authorizationFlow];
        }
    }
}
