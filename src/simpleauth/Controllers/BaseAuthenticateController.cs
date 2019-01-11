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

namespace SimpleAuth.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Api.Profile;
    using Errors;
    using Exceptions;
    using Extensions;
    using Helpers;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Results;
    using Services;
    using Shared;
    using Shared.DTOs;
    using Shared.Events.Openid;
    using Shared.Models;
    using Shared.Repositories;
    using Shared.Requests;
    using Translation;
    using ViewModels;
    using WebSite.Authenticate;
    using WebSite.Authenticate.Common;
    using WebSite.User.Actions;

    public abstract class BaseAuthenticateController : BaseController
    {
        private const string ExternalAuthenticateCookieName = "ExternalAuth-{0}";
        protected const string DefaultLanguage = "en";
        protected readonly IAuthenticateHelper _authenticateHelper;
        protected readonly IAuthenticateActions _authenticateActions;
        private readonly GetResourceOwnerClaimsAction _getResourceOwnerClaims;
        protected readonly IDataProtector _dataProtector;
        private readonly ITranslationManager _translationManager;
        private readonly IUrlHelper _urlHelper;
        private readonly IEventPublisher _eventPublisher;
        private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

        //protected readonly IUserActions _addUser;
        private readonly ITwoFactorAuthenticationHandler _twoFactorAuthenticationHandler;
        private readonly BasicAuthenticateOptions _basicAuthenticateOptions;
        private readonly ISubjectBuilder _subjectBuilder;
        private readonly AddUserOperation _addUser;
        private readonly GetUserOperation _getUserOperation;
        private readonly UpdateUserClaimsOperation _updateUserClaimsOperation;

        public BaseAuthenticateController(
            IAuthenticateActions authenticateActions,
            IDataProtectionProvider dataProtectionProvider,
            ITranslationManager translationManager,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor,
            IEventPublisher eventPublisher,
            IAuthenticationService authenticationService,
            IAuthenticationSchemeProvider authenticationSchemeProvider,
            IAuthenticateHelper authenticateHelper,
            ITwoFactorAuthenticationHandler twoFactorAuthenticationHandler,
            ISubjectBuilder subjectBuilder,
            IProfileRepository profileRepository,
            IResourceOwnerRepository resourceOwnerRepository,
            IEnumerable<IAccountFilter> accountFilters,
            BasicAuthenticateOptions basicAuthenticateOptions) : base(authenticationService)
        {
            _authenticateActions = authenticateActions;
            _getResourceOwnerClaims = new GetResourceOwnerClaimsAction(profileRepository, resourceOwnerRepository);
            _dataProtector = dataProtectionProvider.CreateProtector("Request");
            _translationManager = translationManager;
            _urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
            _eventPublisher = eventPublisher;
            _authenticationSchemeProvider = authenticationSchemeProvider;
            _addUser = new AddUserOperation(resourceOwnerRepository, accountFilters, eventPublisher);
            _getUserOperation = new GetUserOperation(resourceOwnerRepository);
            _updateUserClaimsOperation = new UpdateUserClaimsOperation(resourceOwnerRepository);
            _authenticateHelper = authenticateHelper;
            _basicAuthenticateOptions = basicAuthenticateOptions;
            _twoFactorAuthenticationHandler = twoFactorAuthenticationHandler;
            _subjectBuilder = subjectBuilder;
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Response.Cookies.Delete(CoreConstants.SESSION_ID);
            await _authenticationService
                .SignOutAsync(HttpContext, HostConstants.CookieNames.CookieName, new AuthenticationProperties())
                .ConfigureAwait(false);
            return RedirectToAction("Index", "Authenticate");
        }

        [HttpPost]
        public async Task ExternalLogin(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                throw new ArgumentNullException(nameof(provider));
            }

            var redirectUrl = _urlHelper.Action("LoginCallback", "Authenticate", null, Request.Scheme);
            await _authenticationService.ChallengeAsync(
                    HttpContext,
                    provider,
                    new AuthenticationProperties()
                    {
                        RedirectUri = redirectUrl
                    })
                .ConfigureAwait(false);
        }

        [HttpGet]
        public async Task<IActionResult> LoginCallback(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new SimpleAuthException(ErrorCodes.UnhandledExceptionCode,
                    string.Format(ErrorDescriptions.AnErrorHasBeenRaisedWhenTryingToAuthenticate, error));
            }

            // 1. Get the authenticated user.
            var authenticatedUser = await _authenticationService
                .GetAuthenticatedUser(this, HostConstants.CookieNames.ExternalCookieName)
                .ConfigureAwait(false);
            var resourceOwner =
                await _getResourceOwnerClaims.Execute(authenticatedUser.GetSubject()).ConfigureAwait(false);
            // 2. Automatically create the resource owner.

            var claims = authenticatedUser.Claims.ToList();
            if (resourceOwner != null)
            {
                claims = resourceOwner.Claims.ToList();
            }
            else
            {
                var result = await AddExternalUser(authenticatedUser).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(result.Item1))
                {
                    return RedirectToAction(
                        "Index",
                        "Error",
                        new { code = result.Item2.Value, message = result.Item3 });
                }

                var nameIdentifier = claims.First(c => c.Type == ClaimTypes.NameIdentifier);
                claims.Remove(nameIdentifier);
                claims.Add(new Claim(ClaimTypes.NameIdentifier, result.Item1));
                resourceOwner = await _getResourceOwnerClaims.Execute(result.Item1).ConfigureAwait(false);
            }

            await _authenticationService
                .SignOutAsync(HttpContext,
                    HostConstants.CookieNames.ExternalCookieName,
                    new AuthenticationProperties())
                .ConfigureAwait(false);

            // 3. Two factor authentication.
            if (resourceOwner != null && !string.IsNullOrWhiteSpace(resourceOwner.TwoFactorAuthentication))
            {
                await SetTwoFactorCookie(claims).ConfigureAwait(false);
                try
                {
                    await _authenticateActions.GenerateAndSendCode(resourceOwner.Id).ConfigureAwait(false);
                    //_openIdEventSource.GetConfirmationCode(code);
                    return RedirectToAction("SendCode");
                }
                catch (ClaimRequiredException)
                {
                    return RedirectToAction("SendCode");
                }
            }

            // 4. Set cookie
            await SetLocalCookie(claims.ToOpenidClaims(), Id.Create()).ConfigureAwait(false);
            await _authenticationService
                .SignOutAsync(HttpContext,
                    HostConstants.CookieNames.ExternalCookieName,
                    new AuthenticationProperties())
                .ConfigureAwait(false);

            // 5. Redirect to the profile
            return RedirectToAction("Index", "User");
        }

        [HttpGet]
        public async Task<IActionResult> SendCode(string code)
        {
            // 1. Retrieve user
            var authenticatedUser = await _authenticationService
                .GetAuthenticatedUser(this, HostConstants.CookieNames.TwoFactorCookieName)
                .ConfigureAwait(false);
            if (authenticatedUser?.Identity == null || !authenticatedUser.Identity.IsAuthenticated)
            {
                throw new SimpleAuthException(ErrorCodes.UnhandledExceptionCode,
                    ErrorDescriptions.TwoFactorAuthenticationCannotBePerformed);
            }

            // 2. Return translated view.
            var resourceOwner = await _getUserOperation.Execute(authenticatedUser).ConfigureAwait(false);
            var service = _twoFactorAuthenticationHandler.Get(resourceOwner.TwoFactorAuthentication);
            var viewModel = new CodeViewModel
            {
                AuthRequestCode = code,
                ClaimName = service.RequiredClaim
            };
            var claim = resourceOwner.Claims.FirstOrDefault(c => c.Type == service.RequiredClaim);
            if (claim != null)
            {
                viewModel.ClaimValue = claim.Value;
            }

            ViewBag.IsAuthenticated = false;
            await TranslateView(DefaultLanguage).ConfigureAwait(false);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(CodeViewModel codeViewModel)
        {
            if (codeViewModel == null)
            {
                throw new ArgumentNullException(nameof(codeViewModel));
            }

            ViewBag.IsAuthenticated = false;
            codeViewModel.Validate(ModelState);
            if (!ModelState.IsValid)
            {
                await TranslateView(DefaultLanguage).ConfigureAwait(false);
                return View(codeViewModel);
            }

            // 1. Check user is authenticated
            var authenticatedUser = await _authenticationService
                .GetAuthenticatedUser(this, HostConstants.CookieNames.TwoFactorCookieName)
                .ConfigureAwait(false);
            if (authenticatedUser?.Identity == null || !authenticatedUser.Identity.IsAuthenticated)
            {
                throw new SimpleAuthException(ErrorCodes.UnhandledExceptionCode,
                    ErrorDescriptions.TwoFactorAuthenticationCannotBePerformed);
            }

            // 2. Resend the confirmation code.
            if (codeViewModel.Action == CodeViewModel.RESEND_ACTION)
            {
                await TranslateView(DefaultLanguage).ConfigureAwait(false);
                var resourceOwner = await _getUserOperation.Execute(authenticatedUser).ConfigureAwait(false);
                var claim = resourceOwner.Claims.FirstOrDefault(c => c.Type == codeViewModel.ClaimName);
                if (claim != null)
                {
                    resourceOwner.Claims.Remove(claim);
                }

                resourceOwner.Claims.Add(new Claim(codeViewModel.ClaimName, codeViewModel.ClaimValue));
                var claimsLst = resourceOwner.Claims.Select(c => new Claim(c.Type, c.Value));
                await _updateUserClaimsOperation.Execute(authenticatedUser.GetSubject(), claimsLst)
                    .ConfigureAwait(false);
                await _authenticateActions.GenerateAndSendCode(authenticatedUser.GetSubject())
                    .ConfigureAwait(false);
                return View(codeViewModel);
            }

            // 3. Validate the confirmation code
            if (!await _authenticateActions.ValidateCode(codeViewModel.Code).ConfigureAwait(false))
            {
                await TranslateView(DefaultLanguage).ConfigureAwait(false);
                ModelState.AddModelError("Code", "confirmation code is not valid");
                return View(codeViewModel);
            }

            // 4. Remove the code
            if (!await _authenticateActions.RemoveCode(codeViewModel.Code).ConfigureAwait(false))
            {
                await TranslateView(DefaultLanguage).ConfigureAwait(false);
                ModelState.AddModelError("Code", "an error occured while trying to remove the code");
                return View(codeViewModel);
            }

            // 5. Authenticate the resource owner
            await _authenticationService
                .SignOutAsync(HttpContext,
                    HostConstants.CookieNames.TwoFactorCookieName,
                    new AuthenticationProperties())
                .ConfigureAwait(false);

            // 6. Redirect the user agent
            if (!string.IsNullOrWhiteSpace(codeViewModel.AuthRequestCode))
            {
                var request = _dataProtector.Unprotect<AuthorizationRequest>(codeViewModel.AuthRequestCode);
                await SetLocalCookie(authenticatedUser.Claims, request.SessionId).ConfigureAwait(false);
                var issuerName = Request.GetAbsoluteUriWithVirtualPath();
                var actionResult = await _authenticateHelper.ProcessRedirection(request.ToParameter(),
                        codeViewModel.AuthRequestCode,
                        authenticatedUser.GetSubject(),
                        authenticatedUser.Claims.ToList(),
                        issuerName)
                    .ConfigureAwait(false);
                await LogAuthenticateUser(actionResult, request.ProcessId).ConfigureAwait(false);
                var result = this.CreateRedirectionFromActionResult(actionResult, request);
                return result;
            }

            await SetLocalCookie(authenticatedUser.Claims, Id.Create()).ConfigureAwait(false);

            // 7. Redirect the user agent to the User view.
            return RedirectToAction("Index", "User");
        }

        [HttpGet]
        public async Task<IActionResult> OpenId(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException(nameof(code));
            }

            var authenticatedUser = await SetUser().ConfigureAwait(false);
            var request = _dataProtector.Unprotect<AuthorizationRequest>(code);
            var issuerName = Request.GetAbsoluteUriWithVirtualPath();
            var actionResult = await _authenticateActions.AuthenticateResourceOwnerOpenId(
                    request.ToParameter(),
                    authenticatedUser,
                    code,
                    issuerName)
                .ConfigureAwait(false);
            var result = this.CreateRedirectionFromActionResult(actionResult,
                request);
            if (result != null)
            {
                await LogAuthenticateUser(actionResult, request.ProcessId).ConfigureAwait(false);
                return result;
            }

            await TranslateView(request.UiLocales).ConfigureAwait(false);
            var viewModel = new AuthorizeOpenIdViewModel
            {
                Code = code
            };

            await SetIdProviders(viewModel).ConfigureAwait(false);
            return View(viewModel);
        }

        [HttpPost]
        public async Task ExternalLoginOpenId(string provider, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException(nameof(code));
            }

            // 1. Persist the request code into a cookie & fix the space problems
            var cookieValue = Id.Create();
            ;
            var cookieName = string.Format(ExternalAuthenticateCookieName, cookieValue);
            Response.Cookies.Append(cookieName,
                code,
                new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddMinutes(5)
                });

            // 2. Redirect the User agent
            var redirectUrl =
                _urlHelper.Action("LoginCallbackOpenId", "Authenticate", new { code = cookieValue }, Request.Scheme);
            await _authenticationService.ChallengeAsync(HttpContext,
                    provider,
                    new AuthenticationProperties
                    {
                        RedirectUri = redirectUrl
                    })
                .ConfigureAwait(false);
        }

        [HttpGet]
        public async Task<IActionResult> LoginCallbackOpenId(string code, string error)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException(nameof(code));
            }

            // 1 : retrieve the request from the cookie
            var cookieName = string.Format(ExternalAuthenticateCookieName, code);
            var request = Request.Cookies[string.Format(ExternalAuthenticateCookieName, code)];
            if (request == null)
            {
                throw new SimpleAuthException(ErrorCodes.UnhandledExceptionCode,
                    ErrorDescriptions.TheRequestCannotBeExtractedFromTheCookie);
            }

            // 2 : remove the cookie
            Response.Cookies.Append(cookieName,
                string.Empty,
                new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(-1)
                });

            // 3 : Raise an exception is there's an authentication error
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new SimpleAuthException(ErrorCodes.UnhandledExceptionCode,
                    string.Format(ErrorDescriptions.AnErrorHasBeenRaisedWhenTryingToAuthenticate, error));
            }

            // 4. Check if the user is authenticated
            var authenticatedUser = await _authenticationService
                .GetAuthenticatedUser(this, HostConstants.CookieNames.ExternalCookieName)
                .ConfigureAwait(false);
            if (authenticatedUser == null ||
                !authenticatedUser.Identity.IsAuthenticated ||
                !(authenticatedUser.Identity is ClaimsIdentity))
            {
                throw new SimpleAuthException(ErrorCodes.UnhandledExceptionCode,
                    ErrorDescriptions.TheUserNeedsToBeAuthenticated);
            }

            // 5. Rerieve the claims & insert the resource owner if needed.
            //var claimsIdentity = authenticatedUser.Identity as ClaimsIdentity;
            var claims = authenticatedUser.Claims.ToList();
            var resourceOwner =
                await _getResourceOwnerClaims.Execute(authenticatedUser.GetSubject()).ConfigureAwait(false);
            var sub = string.Empty;
            if (resourceOwner == null)
            {
                var result = await AddExternalUser(authenticatedUser).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(result.Item1))
                {
                    return RedirectToAction(
                        "Index",
                        "Error",
                        new { code = result.Item2.Value, message = result.Item3 });
                }
            }

            if (resourceOwner != null)
            {
                claims = resourceOwner.Claims.ToList();
            }
            else
            {
                var nameIdentifier = claims.First(c => c.Type == ClaimTypes.NameIdentifier);
                claims.Remove(nameIdentifier);
                claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
            }

            var subject = claims.GetSubject();

            if (resourceOwner != null && !string.IsNullOrWhiteSpace(resourceOwner.TwoFactorAuthentication))
            {
                await SetTwoFactorCookie(claims).ConfigureAwait(false);
                await _authenticateActions.GenerateAndSendCode(resourceOwner.Id).ConfigureAwait(false);
                return RedirectToAction("SendCode", new { code = request });
            }

            // 6. Try to authenticate the resource owner & returns the claims.
            var authorizationRequest = _dataProtector.Unprotect<AuthorizationRequest>(request);
            var issuerName = Request.GetAbsoluteUriWithVirtualPath();
            var actionResult = await _authenticateHelper
                .ProcessRedirection(authorizationRequest.ToParameter(), request, subject, claims, issuerName)
                .ConfigureAwait(false);

            // 7. Store claims into new cookie
            if (actionResult != null)
            {
                await SetLocalCookie(claims.ToOpenidClaims(), authorizationRequest.SessionId).ConfigureAwait(false);
                await _authenticationService.SignOutAsync(HttpContext,
                        HostConstants.CookieNames.ExternalCookieName,
                        new AuthenticationProperties())
                    .ConfigureAwait(false);
                await LogAuthenticateUser(actionResult, authorizationRequest.ProcessId).ConfigureAwait(false);
                return this.CreateRedirectionFromActionResult(actionResult, authorizationRequest);
            }

            return RedirectToAction("OpenId", "Authenticate", new { code });
        }

        protected async Task TranslateView(string uiLocales)
        {
            var translations = await _translationManager.GetTranslationsAsync(uiLocales,
                    new List<string>
                    {
                        CoreConstants.StandardTranslationCodes.LoginCode,
                        CoreConstants.StandardTranslationCodes.UserNameCode,
                        CoreConstants.StandardTranslationCodes.PasswordCode,
                        CoreConstants.StandardTranslationCodes.RememberMyLoginCode,
                        CoreConstants.StandardTranslationCodes.LoginLocalAccount,
                        CoreConstants.StandardTranslationCodes.LoginExternalAccount,
                        CoreConstants.StandardTranslationCodes.SendCode,
                        CoreConstants.StandardTranslationCodes.Code,
                        CoreConstants.StandardTranslationCodes.ConfirmCode,
                        CoreConstants.StandardTranslationCodes.SendConfirmationCode,
                        CoreConstants.StandardTranslationCodes.UpdateClaim,
                        CoreConstants.StandardTranslationCodes.ConfirmationCode,
                        CoreConstants.StandardTranslationCodes.ResetConfirmationCode,
                        CoreConstants.StandardTranslationCodes.ValidateConfirmationCode,
                        CoreConstants.StandardTranslationCodes.Phone
                    })
                .ConfigureAwait(false);

            ViewBag.Translations = translations;
        }

        protected async Task SetIdProviders(AuthorizeViewModel authorizeViewModel)
        {
            var schemes =
                (await _authenticationSchemeProvider.GetAllSchemesAsync().ConfigureAwait(false)).Where(p =>
                    !string.IsNullOrWhiteSpace(p.DisplayName));
            var idProviders = new List<IdProviderViewModel>();
            foreach (var scheme in schemes)
            {
                idProviders.Add(new IdProviderViewModel
                {
                    AuthenticationScheme = scheme.Name,
                    DisplayName = scheme.DisplayName
                });
            }

            authorizeViewModel.IdProviders = idProviders;
        }

        protected async Task SetIdProviders(AuthorizeOpenIdViewModel authorizeViewModel)
        {
            var schemes =
                (await _authenticationSchemeProvider.GetAllSchemesAsync().ConfigureAwait(false)).Where(p =>
                    !string.IsNullOrWhiteSpace(p.DisplayName));
            var idProviders = new List<IdProviderViewModel>();
            foreach (var scheme in schemes)
            {
                idProviders.Add(new IdProviderViewModel
                {
                    AuthenticationScheme = scheme.Name,
                    DisplayName = scheme.DisplayName
                });
            }

            authorizeViewModel.IdProviders = idProviders;
        }

        /// <summary>
        /// Add an external account.
        /// </summary>
        /// <param name="authenticatedUser"></param>
        /// <returns></returns>
        private async Task<(string, int?, string)> AddExternalUser(ClaimsPrincipal authenticatedUser)
        {
            var openidClaims = authenticatedUser.Claims.ToOpenidClaims()
                .Where(oc => _basicAuthenticateOptions.ClaimsIncludedInUserCreation.Contains(oc.Type))
                .ToArray();

            var subject = await _subjectBuilder.BuildSubject(openidClaims).ConfigureAwait(false);
            var record = new ResourceOwner //AddUserParameter(subject, Id.Create(), openidClaims)
            {
                Id = subject,
                ExternalLogins = new[] { authenticatedUser.GetSubject() },
                Password = Id.Create().ToSha256Hash(),
                IsLocalAccount = false,
                Claims = openidClaims,
                TwoFactorAuthentication = null,
                CreateDateTime = DateTime.UtcNow,
                UserProfile = new ScimUser
                {
                    Id = subject,
                    Active = true,
                    DisplayName = authenticatedUser.GetPreferredUserName(),
                    Name = new Name
                    {
                        FamilyName = authenticatedUser.GetFamilyName(),
                        GivenName = authenticatedUser.GetGivenName(),
                        MiddleName = authenticatedUser.GetMiddleName()
                    },
                    Emails = new[] { new TypedString { Value = authenticatedUser.GetEmail() } },
                }
            };

            if (!await _addUser.Execute(
                    record,
                    _basicAuthenticateOptions.ScimBaseUrl)
                .ConfigureAwait(false))
            {
                return (null, (int)HttpStatusCode.Conflict, "Failed to add user");
            }

            record.Password = string.Empty;
            await _eventPublisher.Publish(
                    new ExternalUserCreated(
                        Id.Create(),
                        record,
                        DateTime.UtcNow))
                .ConfigureAwait(false);
            return (subject, null, null);
        }

        protected async Task LogAuthenticateUser(EndpointResult act, string processId)
        {
            if (string.IsNullOrWhiteSpace(processId))
            {
                return;
            }

            await _eventPublisher.Publish(
                    new ResourceOwnerAuthenticated(
                        Id.Create(),
                        processId,
                        act,
                        DateTime.UtcNow))
                .ConfigureAwait(false);
        }

        protected async Task SetLocalCookie(IEnumerable<Claim> claims, string sessionId)
        {
            var cls = claims.ToList();
            var tokenValidity = TimeSpan.FromHours(1d); //_configurationService.TokenValidityPeriod;
            var now = DateTime.UtcNow;
            var expires = now.Add(tokenValidity);
            Response.Cookies.Append(
                CoreConstants.SESSION_ID,
                sessionId,
                new CookieOptions
                {
                    HttpOnly = false,
                    Expires = expires,
                    SameSite = SameSiteMode.None
                });
            var identity = new ClaimsIdentity(cls, HostConstants.CookieNames.CookieName);
            var principal = new ClaimsPrincipal(identity);
            await _authenticationService.SignInAsync(
                    HttpContext,
                    HostConstants.CookieNames.CookieName,
                    principal,
                    new AuthenticationProperties
                    {
                        IssuedUtc = now,
                        ExpiresUtc = expires,
                        AllowRefresh = false,
                        IsPersistent = false
                    })
                .ConfigureAwait(false);
        }

        protected async Task SetTwoFactorCookie(IEnumerable<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims, HostConstants.CookieNames.TwoFactorCookieName);
            var principal = new ClaimsPrincipal(identity);
            await _authenticationService.SignInAsync(
                    HttpContext,
                    HostConstants.CookieNames.TwoFactorCookieName,
                    principal,
                    new AuthenticationProperties
                    {
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(5),
                        IsPersistent = false
                    })
                .ConfigureAwait(false);
        }
    }
}
