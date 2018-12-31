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

namespace SimpleAuth.Server.Controllers
{
    using Errors;
    using Exceptions;
    using Extensions;
    using Logging;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Results;
    using Shared;
    using Shared.Events.Openid;
    using Shared.Models;
    using Shared.Requests;
    using SimpleAuth;
    using SimpleAuth.Api.Profile;
    using SimpleAuth.Extensions;
    using SimpleAuth.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Api.Profile.Actions;
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
        private readonly IGetResourceOwnerClaimsAction _profileActions;
        protected readonly IDataProtector _dataProtector;
        private readonly ITranslationManager _translationManager;
        protected readonly IOpenIdEventSource _openIdEventSource;
        private readonly IUrlHelper _urlHelper;
        private readonly IEventPublisher _eventPublisher;
        private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

        //protected readonly IUserActions _userActions;
        private readonly OAuthConfigurationOptions _configurationService;
        private readonly ITwoFactorAuthenticationHandler _twoFactorAuthenticationHandler;
        private readonly BasicAuthenticateOptions _basicAuthenticateOptions;
        private readonly ISubjectBuilder _subjectBuilder;
        private readonly IAddUserOperation _userActions;
        private readonly IGetUserOperation _getUserOperation;
        private readonly IUpdateUserClaimsOperation _updateUserClaimsOperation;

        public BaseAuthenticateController(
            IAuthenticateActions authenticateActions,
            IGetResourceOwnerClaimsAction profileActions,
            IDataProtectionProvider dataProtectionProvider,
            ITranslationManager translationManager,
            IOpenIdEventSource openIdEventSource,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor,
            IEventPublisher eventPublisher,
            IAuthenticationService authenticationService,
            IAuthenticationSchemeProvider authenticationSchemeProvider,
            IAddUserOperation userActions,
            IGetUserOperation getUserOperation,
            IUpdateUserClaimsOperation updateUserClaimsOperation,
            OAuthConfigurationOptions configurationService,
            IAuthenticateHelper authenticateHelper,
            ITwoFactorAuthenticationHandler twoFactorAuthenticationHandler,
            ISubjectBuilder subjectBuilder,
            BasicAuthenticateOptions basicAuthenticateOptions) : base(authenticationService)
        {
            _authenticateActions = authenticateActions;
            _profileActions = profileActions;
            _dataProtector = dataProtectionProvider.CreateProtector("Request");
            _translationManager = translationManager;
            _openIdEventSource = openIdEventSource;
            _urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
            _eventPublisher = eventPublisher;
            _authenticationSchemeProvider = authenticationSchemeProvider;
            _userActions = userActions;
            _getUserOperation = getUserOperation;
            _updateUserClaimsOperation = updateUserClaimsOperation;
            _configurationService = configurationService;
            _authenticateHelper = authenticateHelper;
            _basicAuthenticateOptions = basicAuthenticateOptions;
            _twoFactorAuthenticationHandler = twoFactorAuthenticationHandler;
            _subjectBuilder = subjectBuilder;
            Check();
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

            var redirectUrl = _urlHelper.AbsoluteAction("LoginCallback", "Authenticate");
            await _authenticationService.ChallengeAsync(HttpContext,
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
                await _profileActions.Execute(authenticatedUser.GetSubject()).ConfigureAwait(false);
            string sub = null;

            // 2. Automatically create the resource owner.
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

            var claims = authenticatedUser.Claims.ToList();
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
                    var code = await _authenticateActions.GenerateAndSendCode(resourceOwner.Id).ConfigureAwait(false);
                    _openIdEventSource.GetConfirmationCode(code);
                    return RedirectToAction("SendCode");
                }
                catch (ClaimRequiredException)
                {
                    return RedirectToAction("SendCode");
                }
            }

            // 4. Set cookie
            await SetLocalCookie(claims.ToOpenidClaims(), Guid.NewGuid().ToString()).ConfigureAwait(false);
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
                var claimsLst = resourceOwner.Claims.Select(c => new ClaimAggregate(c.Type, c.Value));
                await _updateUserClaimsOperation.Execute(authenticatedUser.GetSubject(), claimsLst)
                    .ConfigureAwait(false);
                var code = await _authenticateActions.GenerateAndSendCode(authenticatedUser.GetSubject())
                    .ConfigureAwait(false);
                _openIdEventSource.GetConfirmationCode(code);
                return View(codeViewModel);
            }

            // 3. Validate the confirmation code
            if (!await _authenticateActions.ValidateCode(codeViewModel.Code).ConfigureAwait(false))
            {
                await TranslateView(DefaultLanguage).ConfigureAwait(false);
                ModelState.AddModelError("Code", "confirmation code is not valid");
                _openIdEventSource.ConfirmationCodeNotValid(codeViewModel.Code);
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
                var issuerName = HttpRequestsExtensions.GetAbsoluteUriWithVirtualPath(Request);
                var actionResult = await _authenticateHelper.ProcessRedirection(request.ToParameter(),
                        codeViewModel.AuthRequestCode,
                        authenticatedUser.GetSubject(),
                        authenticatedUser.Claims.ToList(),
                        issuerName)
                    .ConfigureAwait(false);
                LogAuthenticateUser(actionResult, request.ProcessId);
                var result = this.CreateRedirectionFromActionResult(actionResult, request);
                return result;
            }

            await SetLocalCookie(authenticatedUser.Claims, Guid.NewGuid().ToString()).ConfigureAwait(false);

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
            var issuerName = HttpRequestsExtensions.GetAbsoluteUriWithVirtualPath(Request);
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
                LogAuthenticateUser(actionResult, request.ProcessId);
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
            var cookieValue = Guid.NewGuid().ToString();
            var cookieName = string.Format(ExternalAuthenticateCookieName, cookieValue);
            Response.Cookies.Append(cookieName,
                code,
                new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddMinutes(5)
                });

            // 2. Redirect the User agent
            var redirectUrl =
                _urlHelper.AbsoluteAction("LoginCallbackOpenId", "Authenticate", new { code = cookieValue });
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
                await _profileActions.Execute(authenticatedUser.GetSubject()).ConfigureAwait(false);
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
                var confirmationCode =
                    await _authenticateActions.GenerateAndSendCode(resourceOwner.Id).ConfigureAwait(false);
                _openIdEventSource.GetConfirmationCode(confirmationCode);
                return RedirectToAction("SendCode", new { code = request });
            }

            // 6. Try to authenticate the resource owner & returns the claims.
            var authorizationRequest = _dataProtector.Unprotect<AuthorizationRequest>(request);
            var issuerName = HttpRequestsExtensions.GetAbsoluteUriWithVirtualPath(Request);
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
                LogAuthenticateUser(actionResult, authorizationRequest.ProcessId);
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

                .Where(oc => _basicAuthenticateOptions.ClaimsIncludedInUserCreation.Contains(oc.Type));

            var subject = await _subjectBuilder.BuildSubject(authenticatedUser.Claims.ToArray()).ConfigureAwait(false);
            var record = new ResourceOwner //AddUserParameter(subject, Guid.NewGuid().ToString("N"), openidClaims)
            {
                Id = subject,
                Password = Guid.NewGuid().ToString("N"),
                IsLocalAccount = false,
                Claims = openidClaims.ToArray()
            };

            if (!await _userActions.Execute(
                    record,
                    _basicAuthenticateOptions.ScimBaseUrl)
                .ConfigureAwait(false))
            {
                return (null, (int)HttpStatusCode.Conflict, "Failed to add user");
            }

            return (subject, null, null);
        }

        protected void Check()
        {
            if (_basicAuthenticateOptions.ScimBaseUrl != null
                && (string.IsNullOrWhiteSpace(_basicAuthenticateOptions.AuthorizationWellKnownConfiguration)
                    || string.IsNullOrWhiteSpace(_basicAuthenticateOptions.ClientId)
                    || string.IsNullOrWhiteSpace(_basicAuthenticateOptions.ClientSecret)
                    || _basicAuthenticateOptions.ScimBaseUrl == null))
            {
                throw new SimpleAuthException(ErrorCodes.InternalError,
                    ErrorDescriptions.TheScimConfigurationMustBeSpecified);
            }
        }

        protected void LogAuthenticateUser(EndpointResult act, string processId)
        {
            if (string.IsNullOrWhiteSpace(processId))
            {
                return;
            }

            _eventPublisher.Publish(
                new ResourceOwnerAuthenticated(
                Guid.NewGuid().ToString(),
                processId,
                act,
                2));
        }

        protected async Task SetLocalCookie(IEnumerable<Claim> claims, string sessionId)
        {
            var cls = claims.ToList();
            var tokenValidity = _configurationService.TokenValidityPeriod;
            var now = DateTime.UtcNow;
            var expires = now.Add(tokenValidity);
            HttpContext.Response.Cookies.Append(CoreConstants.SESSION_ID,
                sessionId,
                new CookieOptions
                {
                    HttpOnly = false,
                    Expires = expires,
                    SameSite = SameSiteMode.None
                });
            var identity = new ClaimsIdentity(cls, HostConstants.CookieNames.CookieName);
            var principal = new ClaimsPrincipal(identity);
            await _authenticationService.SignInAsync(HttpContext,
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
            await _authenticationService.SignInAsync(HttpContext,
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
