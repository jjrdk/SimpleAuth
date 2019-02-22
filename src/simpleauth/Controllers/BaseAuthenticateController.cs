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
    using Exceptions;
    using Extensions;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Shared;
    using Shared.Events.Openid;
    using Shared.Models;
    using Shared.Repositories;
    using Shared.Requests;
    using SimpleAuth.Shared.Errors;
    using SimpleAuth.WebSite.Authenticate;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using ViewModels;
    using WebSite.User.Actions;

    /// <summary>
    /// Defines the base authentication controller.
    /// </summary>
    /// <seealso cref="SimpleAuth.Controllers.BaseController" />
    public abstract class BaseAuthenticateController : BaseController
    {
        private const string ExternalAuthenticateCookieName = "ExternalAuth-{0}";
        private readonly GenerateAndSendCodeAction _generateAndSendCode;
        private readonly ValidateConfirmationCodeAction _validateConfirmationCode;
        private readonly AuthenticateResourceOwnerOpenIdAction _authenticateResourceOwnerOpenId;
        private readonly AuthenticateHelper _authenticateHelper;
        protected readonly IDataProtector _dataProtector;
        private readonly IUrlHelper _urlHelper;
        private readonly IEventPublisher _eventPublisher;
        private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

        //protected readonly IUserActions _addUser;
        private readonly ITwoFactorAuthenticationHandler _twoFactorAuthenticationHandler;
        private readonly RuntimeSettings _runtimeSettings;
        private readonly ISubjectBuilder _subjectBuilder;
        private readonly IResourceOwnerStore _resourceOwnerRepository;
        private readonly IConfirmationCodeStore _confirmationCodeStore;
        private readonly AddUserOperation _addUser;
        private readonly GetUserOperation _getUserOperation;
        private readonly UpdateUserClaimsOperation _updateUserClaimsOperation;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseAuthenticateController"/> class.
        /// </summary>
        /// <param name="dataProtectionProvider">The data protection provider.</param>
        /// <param name="urlHelperFactory">The URL helper factory.</param>
        /// <param name="actionContextAccessor">The action context accessor.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="authenticationService">The authentication service.</param>
        /// <param name="authenticationSchemeProvider">The authentication scheme provider.</param>
        /// <param name="twoFactorAuthenticationHandler">The two factor authentication handler.</param>
        /// <param name="authorizationCodeStore">The authorization code store.</param>
        /// <param name="subjectBuilder">The subject builder.</param>
        /// <param name="consentRepository">The consent repository.</param>
        /// <param name="scopeRepository">The scope repository.</param>
        /// <param name="tokenStore">The token store.</param>
        /// <param name="resourceOwnerRepository">The resource owner repository.</param>
        /// <param name="confirmationCodeStore">The confirmation code store.</param>
        /// <param name="clientStore">The client store.</param>
        /// <param name="accountFilters">The account filters.</param>
        /// <param name="runtimeSettings">The runtime settings.</param>
        public BaseAuthenticateController(
            IDataProtectionProvider dataProtectionProvider,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor,
            IEventPublisher eventPublisher,
            IAuthenticationService authenticationService,
            IAuthenticationSchemeProvider authenticationSchemeProvider,
            ITwoFactorAuthenticationHandler twoFactorAuthenticationHandler,
            IAuthorizationCodeStore authorizationCodeStore,
            ISubjectBuilder subjectBuilder,
            IConsentRepository consentRepository,
            IScopeRepository scopeRepository,
            ITokenStore tokenStore,
            IResourceOwnerRepository resourceOwnerRepository,
            IConfirmationCodeStore confirmationCodeStore,
            IClientStore clientStore,
            IEnumerable<IAccountFilter> accountFilters,
            RuntimeSettings runtimeSettings)
            : base(authenticationService)
        {
            _generateAndSendCode = new GenerateAndSendCodeAction(
                resourceOwnerRepository,
                confirmationCodeStore,
                twoFactorAuthenticationHandler);
            _validateConfirmationCode = new ValidateConfirmationCodeAction(confirmationCodeStore);
            _authenticateHelper = new AuthenticateHelper(
                authorizationCodeStore,
                tokenStore,
                scopeRepository,
                consentRepository,
                clientStore,
                eventPublisher);
            _authenticateResourceOwnerOpenId = new AuthenticateResourceOwnerOpenIdAction(
                authorizationCodeStore,
                tokenStore,
                scopeRepository,
                consentRepository,
                clientStore,
                eventPublisher);
            _dataProtector = dataProtectionProvider.CreateProtector("Request");
            _urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
            _eventPublisher = eventPublisher;
            _authenticationSchemeProvider = authenticationSchemeProvider;
            _addUser = new AddUserOperation(resourceOwnerRepository, accountFilters, eventPublisher);
            _getUserOperation = new GetUserOperation(resourceOwnerRepository);
            _updateUserClaimsOperation = new UpdateUserClaimsOperation(resourceOwnerRepository);
            _runtimeSettings = runtimeSettings;
            _twoFactorAuthenticationHandler = twoFactorAuthenticationHandler;
            _subjectBuilder = subjectBuilder;
            _resourceOwnerRepository = resourceOwnerRepository;
            _confirmationCodeStore = confirmationCodeStore;
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Response.Cookies.Delete(CoreConstants.SessionId);
            await _authenticationService.SignOutAsync(
                    HttpContext,
                    CookieNames.CookieName,
                    new AuthenticationProperties())
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
                    new AuthenticationProperties() { RedirectUri = redirectUrl })
                .ConfigureAwait(false);
        }

        [HttpGet]
        public async Task<IActionResult> LoginCallback(string error, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new SimpleAuthException(
                    ErrorCodes.UnhandledExceptionCode,
                    string.Format(ErrorDescriptions.AnErrorHasBeenRaisedWhenTryingToAuthenticate, error));
            }

            // 1. Get the authenticated user.
            var authenticatedUser = await _authenticationService
                .GetAuthenticatedUser(this, CookieNames.ExternalCookieName)
                .ConfigureAwait(false);
            if (authenticatedUser == null)
            {
                return RedirectToAction("Index", "Authenticate");
            }
            var resourceOwner = await _resourceOwnerRepository.Get(
                    new ExternalAccountLink { Issuer = authenticatedUser.Identity.AuthenticationType, Subject = authenticatedUser.GetSubject() }, cancellationToken)
                .ConfigureAwait(false);
            // 2. Automatically create the resource owner.

            var claims = authenticatedUser.Claims.ToList();
            if (resourceOwner != null)
            {
                claims = resourceOwner.Claims.ToList();
            }
            else
            {
                var result = await AddExternalUser(authenticatedUser, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(result.Item1))
                {
                    return RedirectToAction("Index", "Error", new { code = result.Item2.Value, message = result.Item3 });
                }

                var nameIdentifier = claims.First(c => c.Type == ClaimTypes.NameIdentifier);
                claims.Remove(nameIdentifier);
                claims.Add(new Claim(ClaimTypes.NameIdentifier, result.Item1));
                resourceOwner = await _resourceOwnerRepository.Get(result.Item1, cancellationToken)
                    .ConfigureAwait(false);
            }

            await _authenticationService.SignOutAsync(
                    HttpContext,
                    CookieNames.ExternalCookieName,
                    new AuthenticationProperties())
                .ConfigureAwait(false);

            // 3. Two factor authentication.
            if (resourceOwner != null && !string.IsNullOrWhiteSpace(resourceOwner.TwoFactorAuthentication))
            {
                await SetTwoFactorCookie(claims.ToArray()).ConfigureAwait(false);
                try
                {
                    await _generateAndSendCode.Send(resourceOwner.Id, cancellationToken).ConfigureAwait(false);
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
            await _authenticationService.SignOutAsync(
                    HttpContext,
                    CookieNames.ExternalCookieName,
                    new AuthenticationProperties())
                .ConfigureAwait(false);

            // 5. Redirect to the profile
            return RedirectToAction("Index", "User");
        }

        [HttpGet]
        public async Task<IActionResult> SendCode(string code, CancellationToken cancellationToken)
        {
            // 1. Retrieve user
            var authenticatedUser = await _authenticationService
                .GetAuthenticatedUser(this, CookieNames.TwoFactorCookieName)
                .ConfigureAwait(false);
            if (authenticatedUser?.Identity == null || !authenticatedUser.Identity.IsAuthenticated)
            {
                throw new SimpleAuthException(
                    ErrorCodes.UnhandledExceptionCode,
                    ErrorDescriptions.TwoFactorAuthenticationCannotBePerformed);
            }

            // 2. Return translated view.
            var resourceOwner =
                await _getUserOperation.Execute(authenticatedUser, cancellationToken).ConfigureAwait(false);
            var service = _twoFactorAuthenticationHandler.Get(resourceOwner.TwoFactorAuthentication);
            var viewModel = new CodeViewModel { AuthRequestCode = code, ClaimName = service.RequiredClaim };
            var claim = resourceOwner.Claims.FirstOrDefault(c => c.Type == service.RequiredClaim);
            if (claim != null)
            {
                viewModel.ClaimValue = claim.Value;
            }

            ViewBag.IsAuthenticated = false;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(CodeViewModel codeViewModel, CancellationToken cancellationToken)
        {
            if (codeViewModel == null)
            {
                throw new ArgumentNullException(nameof(codeViewModel));
            }

            ViewBag.IsAuthenticated = false;
            codeViewModel.Validate(ModelState);
            if (!ModelState.IsValid)
            {
                return View(codeViewModel);
            }

            // 1. Check user is authenticated
            var authenticatedUser = await _authenticationService
                .GetAuthenticatedUser(this, CookieNames.TwoFactorCookieName)
                .ConfigureAwait(false);
            if (authenticatedUser?.Identity?.IsAuthenticated != true)
            {
                throw new SimpleAuthException(
                    ErrorCodes.UnhandledExceptionCode,
                    ErrorDescriptions.TwoFactorAuthenticationCannotBePerformed);
            }

            // 2. Resend the confirmation code.
            if (codeViewModel.Action == CodeViewModel.ResendAction)
            {
                var resourceOwner = await _getUserOperation.Execute(authenticatedUser, cancellationToken)
                    .ConfigureAwait(false);
                var claim = resourceOwner.Claims.FirstOrDefault(c => c.Type == codeViewModel.ClaimName);
                if (claim != null)
                {
                    resourceOwner.Claims.Remove(claim);
                }

                resourceOwner.Claims = resourceOwner.Claims.Add(new Claim(codeViewModel.ClaimName, codeViewModel.ClaimValue));
                var claimsLst = resourceOwner.Claims.Select(c => new Claim(c.Type, c.Value));
                await _updateUserClaimsOperation.Execute(authenticatedUser.GetSubject(), claimsLst, cancellationToken)
                    .ConfigureAwait(false);
                await _generateAndSendCode.Send(authenticatedUser.GetSubject(), cancellationToken)
                    .ConfigureAwait(false);
                return View(codeViewModel);
            }

            // 3. Validate the confirmation code
            if (!await _validateConfirmationCode.Execute(codeViewModel.Code).ConfigureAwait(false))
            {
                ModelState.AddModelError("Code", "confirmation code is not valid");
                return View(codeViewModel);
            }

            // 4. Remove the code
            if (string.IsNullOrWhiteSpace(codeViewModel.Code)
                || !await _confirmationCodeStore.Remove(codeViewModel.Code).ConfigureAwait(false))
            {
                ModelState.AddModelError("Code", "an error occured while trying to remove the code");
                return View(codeViewModel);
            }

            // 5. Authenticate the resource owner
            await _authenticationService.SignOutAsync(
                    HttpContext,
                    CookieNames.TwoFactorCookieName,
                    new AuthenticationProperties())
                .ConfigureAwait(false);

            // 6. Redirect the user agent
            var authenticatedUserClaims = authenticatedUser.Claims.ToArray();
            if (!string.IsNullOrWhiteSpace(codeViewModel.AuthRequestCode))
            {
                var request = _dataProtector.Unprotect<AuthorizationRequest>(codeViewModel.AuthRequestCode);
                await SetLocalCookie(authenticatedUserClaims, request.session_id).ConfigureAwait(false);
                var issuerName = Request.GetAbsoluteUriWithVirtualPath();
                var actionResult = await _authenticateHelper.ProcessRedirection(
                        request.ToParameter(),
                        codeViewModel.AuthRequestCode,
                        authenticatedUser.GetSubject(),
                        authenticatedUserClaims,
                        issuerName,
                        cancellationToken)
                    .ConfigureAwait(false);
                await LogAuthenticateUser(authenticatedUser.GetSubject(), actionResult.Amr, request.aggregate_id).ConfigureAwait(false);
                var result = this.CreateRedirectionFromActionResult(actionResult, request);
                return result;
            }

            await SetLocalCookie(authenticatedUserClaims, Id.Create()).ConfigureAwait(false);

            // 7. Redirect the user agent to the User view.
            return RedirectToAction("Index", "User");
        }

        [HttpGet]
        public async Task<IActionResult> OpenId(string code, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException(nameof(code));
            }

            var authenticatedUser = await SetUser().ConfigureAwait(false);
            var request = _dataProtector.Unprotect<AuthorizationRequest>(code);
            var issuerName = Request.GetAbsoluteUriWithVirtualPath();
            var actionResult = await _authenticateResourceOwnerOpenId.Execute(
                    request.ToParameter(),
                    authenticatedUser,
                    code,
                    issuerName,
                    cancellationToken)
                .ConfigureAwait(false);
            var result = this.CreateRedirectionFromActionResult(actionResult, request);
            if (result != null)
            {
                await LogAuthenticateUser(authenticatedUser.GetSubject(), actionResult.Amr, request.aggregate_id).ConfigureAwait(false);
                return result;
            }

            var viewModel = new AuthorizeOpenIdViewModel { Code = code };

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
            Response.Cookies.Append(cookieName, code, new CookieOptions { Expires = DateTime.UtcNow.AddMinutes(5) });

            // 2. Redirect the User agent
            var redirectUrl = _urlHelper.Action(
                "LoginCallbackOpenId",
                "Authenticate",
                new { code = cookieValue },
                Request.Scheme);
            await _authenticationService.ChallengeAsync(
                    HttpContext,
                    provider,
                    new AuthenticationProperties { RedirectUri = redirectUrl })
                .ConfigureAwait(false);
        }

        [HttpGet]
        public async Task<IActionResult> LoginCallbackOpenId(
            string code,
            string error,
            CancellationToken cancellationToken)
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
                throw new SimpleAuthException(
                    ErrorCodes.UnhandledExceptionCode,
                    ErrorDescriptions.TheRequestCannotBeExtractedFromTheCookie);
            }

            // 2 : remove the cookie
            Response.Cookies.Append(
                cookieName,
                string.Empty,
                new CookieOptions { Expires = DateTime.UtcNow.AddDays(-1) });

            // 3 : Raise an exception is there's an authentication error
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new SimpleAuthException(
                    ErrorCodes.UnhandledExceptionCode,
                    string.Format(ErrorDescriptions.AnErrorHasBeenRaisedWhenTryingToAuthenticate, error));
            }

            // 4. Check if the user is authenticated
            var authenticatedUser = await _authenticationService
                .GetAuthenticatedUser(this, CookieNames.ExternalCookieName)
                .ConfigureAwait(false);
            if (authenticatedUser == null
                || !authenticatedUser.Identity.IsAuthenticated
                || !(authenticatedUser.Identity is ClaimsIdentity))
            {
                throw new SimpleAuthException(
                    ErrorCodes.UnhandledExceptionCode,
                    ErrorDescriptions.TheUserNeedsToBeAuthenticated);
            }

            // 5. Rerieve the claims & insert the resource owner if needed.
            //var claimsIdentity = authenticatedUser.Identity as ClaimsIdentity;
            var claims = authenticatedUser.Claims.ToArray();
            var resourceOwner = await _resourceOwnerRepository.Get(authenticatedUser.GetSubject(), cancellationToken)
                .ConfigureAwait(false);
            var sub = string.Empty;
            if (resourceOwner == null)
            {
                var result = await AddExternalUser(authenticatedUser, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(result.Item1))
                {
                    return RedirectToAction("Index", "Error", new { code = result.Item2.Value, message = result.Item3 });
                }
            }

            if (resourceOwner != null)
            {
                claims = resourceOwner.Claims;
            }
            else
            {
                var nameIdentifier = claims.First(c => c.Type == ClaimTypes.NameIdentifier);
                claims = claims.Remove(nameIdentifier);
                claims = claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
            }

            var subject = claims.GetSubject();

            if (resourceOwner != null && !string.IsNullOrWhiteSpace(resourceOwner.TwoFactorAuthentication))
            {
                await SetTwoFactorCookie(claims).ConfigureAwait(false);
                await _generateAndSendCode.Send(resourceOwner.Id, cancellationToken).ConfigureAwait(false);
                return RedirectToAction("SendCode", new { code = request });
            }

            // 6. Try to authenticate the resource owner & returns the claims.
            var authorizationRequest = _dataProtector.Unprotect<AuthorizationRequest>(request);
            var issuerName = Request.GetAbsoluteUriWithVirtualPath();
            var actionResult = await _authenticateHelper.ProcessRedirection(
                    authorizationRequest.ToParameter(),
                    request,
                    subject,
                    claims,
                    issuerName,
                    cancellationToken)
                .ConfigureAwait(false);

            // 7. Store claims into new cookie
            if (actionResult != null)
            {
                await SetLocalCookie(claims.ToOpenidClaims(), authorizationRequest.session_id).ConfigureAwait(false);
                await _authenticationService.SignOutAsync(
                        HttpContext,
                        CookieNames.ExternalCookieName,
                        new AuthenticationProperties())
                    .ConfigureAwait(false);
                await LogAuthenticateUser(subject, actionResult.Amr, authorizationRequest.aggregate_id).ConfigureAwait(false);
                return this.CreateRedirectionFromActionResult(actionResult, authorizationRequest);
            }

            return RedirectToAction("OpenId", "Authenticate", new { code });
        }

        /// <summary>
        /// Sets the identifier providers.
        /// </summary>
        /// <param name="authorizeViewModel">The authorize view model.</param>
        /// <returns></returns>
        protected async Task SetIdProviders(IdProviderAuthorizeViewModel authorizeViewModel)
        {
            var schemes =
                (await _authenticationSchemeProvider.GetAllSchemesAsync().ConfigureAwait(false)).Where(
                    p => !string.IsNullOrWhiteSpace(p.DisplayName));
            var idProviders = schemes.Select(
                    scheme => new IdProviderViewModel
                    {
                        AuthenticationScheme = scheme.Name,
                        DisplayName = scheme.DisplayName
                    })
                .ToList();

            authorizeViewModel.IdProviders = idProviders;
        }

        internal async Task LogAuthenticateUser(string resourceOwner, string amr, string processId)
        {
            if (string.IsNullOrWhiteSpace(processId))
            {
                return;
            }

            await _eventPublisher.Publish(new ResourceOwnerAuthenticated(Id.Create(), processId, resourceOwner, amr, DateTime.UtcNow))
                .ConfigureAwait(false);
        }

        protected async Task SetLocalCookie(Claim[] claims, string sessionId)
        {
            var tokenValidity = TimeSpan.FromHours(1d); //_configurationService.TokenValidityPeriod;
            var now = DateTime.UtcNow;
            var expires = now.Add(tokenValidity);
            Response.Cookies.Append(
                CoreConstants.SessionId,
                sessionId,
                new CookieOptions { HttpOnly = false, Expires = expires, SameSite = SameSiteMode.None });
            var identity = new ClaimsIdentity(claims, CookieNames.CookieName);
            var principal = new ClaimsPrincipal(identity);
            await _authenticationService.SignInAsync(
                    HttpContext,
                    CookieNames.CookieName,
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

        protected async Task SetTwoFactorCookie(Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, CookieNames.TwoFactorCookieName);
            var principal = new ClaimsPrincipal(identity);
            await _authenticationService.SignInAsync(
                    HttpContext,
                    CookieNames.TwoFactorCookieName,
                    principal,
                    new AuthenticationProperties { ExpiresUtc = DateTime.UtcNow.AddMinutes(5), IsPersistent = false })
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Add an external account.
        /// </summary>
        /// <param name="authenticatedUser"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<(string, int?, string)> AddExternalUser(
            ClaimsPrincipal authenticatedUser,
            CancellationToken cancellationToken)
        {
            var openidClaims = authenticatedUser.Claims
                .Where(oc => _runtimeSettings.ClaimsIncludedInUserCreation.Contains(oc.Type))
                .ToOpenidClaims()
                .ToArray();

            var subject = await _subjectBuilder.BuildSubject(openidClaims).ConfigureAwait(false);
            var record = new ResourceOwner //AddUserParameter(subject, ClientId.Create(), openidClaims)
            {
                Id = subject,
                ExternalLogins =
                    new[]
                    {
                        new ExternalAccountLink
                        {
                            Subject = authenticatedUser.GetSubject(),
                            Issuer = authenticatedUser.Identity.AuthenticationType
                        }
                    },
                Password = Id.Create().ToSha256Hash(),
                IsLocalAccount = false,
                Claims = openidClaims,
                TwoFactorAuthentication = null,
                CreateDateTime = DateTime.UtcNow
            };

            if (!await _addUser.Execute(record, cancellationToken).ConfigureAwait(false))
            {
                return (null, (int)HttpStatusCode.Conflict, "Failed to add user");
            }

            record.Password = string.Empty;
            await _eventPublisher.Publish(new ExternalUserCreated(Id.Create(), record, DateTime.UtcNow))
                .ConfigureAwait(false);
            return (subject, null, null);
        }
    }
}
