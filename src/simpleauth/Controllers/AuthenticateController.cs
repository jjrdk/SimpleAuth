﻿namespace SimpleAuth.Controllers
{
    using Exceptions;
    using Extensions;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Parameters;
    using Shared;
    using Shared.Repositories;
    using Shared.Requests;
    using SimpleAuth.Shared.Errors;
    using SimpleAuth.Shared.Events.Logging;
    using SimpleAuth.WebSite.Authenticate;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using SimpleAuth.Filters;
    using ViewModels;

    /// <summary>
    /// Defines the authentication controller.
    /// </summary>
    /// <seealso cref="BaseAuthenticateController" />
    [ThrottleFilter]
    public class AuthenticateController : BaseAuthenticateController
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly IAuthenticateResourceOwnerService[] _resourceOwnerServices;
        private readonly LocalOpenIdUserAuthenticationAction _localOpenIdAuthentication;
        private readonly GenerateAndSendCodeAction _generateAndSendCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticateController"/> class.
        /// </summary>
        /// <param name="dataProtectionProvider">The data protection provider.</param>
        /// <param name="urlHelperFactory">The URL helper factory.</param>
        /// <param name="actionContextAccessor">The action context accessor.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="authenticationService">The authentication service.</param>
        /// <param name="authenticationSchemeProvider">The authentication scheme provider.</param>
        /// <param name="resourceOwnerServices">The resource owner services.</param>
        /// <param name="twoFactorAuthenticationHandler">The two factor authentication handler.</param>
        /// <param name="subjectBuilder">The subject builder.</param>
        /// <param name="authorizationCodeStore">The authorization code store.</param>
        /// <param name="scopeRepository">The scope repository.</param>
        /// <param name="tokenStore">The token store.</param>
        /// <param name="consentRepository">The consent repository.</param>
        /// <param name="confirmationCodeStore">The confirmation code store.</param>
        /// <param name="clientStore">The client store.</param>
        /// <param name="resourceOwnerRepository">The resource owner repository.</param>
        /// <param name="jwksStore"></param>
        /// <param name="accountFilters">The account filters.</param>
        /// <param name="runtimeSettings">The runtime settings.</param>
        public AuthenticateController(
            IDataProtectionProvider dataProtectionProvider,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor,
            IEventPublisher eventPublisher,
            IAuthenticationService authenticationService,
            IAuthenticationSchemeProvider authenticationSchemeProvider,
            IEnumerable<IAuthenticateResourceOwnerService> resourceOwnerServices,
            ITwoFactorAuthenticationHandler twoFactorAuthenticationHandler,
            ISubjectBuilder subjectBuilder,
            IAuthorizationCodeStore authorizationCodeStore,
            IScopeRepository scopeRepository,
            ITokenStore tokenStore,
            IConsentRepository consentRepository,
            IConfirmationCodeStore confirmationCodeStore,
            IClientStore clientStore,
            IResourceOwnerRepository resourceOwnerRepository,
            IJwksStore jwksStore,
            IEnumerable<AccountFilter> accountFilters,
            RuntimeSettings runtimeSettings)
            : base(
                dataProtectionProvider,
                urlHelperFactory,
                actionContextAccessor,
                eventPublisher,
                authenticationService,
                authenticationSchemeProvider,
                twoFactorAuthenticationHandler,
                authorizationCodeStore,
                consentRepository,
                scopeRepository,
                tokenStore,
                resourceOwnerRepository,
                confirmationCodeStore,
                clientStore,
                jwksStore,
                subjectBuilder,
                accountFilters,
                runtimeSettings)
        {
            _eventPublisher = eventPublisher;
            var services = resourceOwnerServices.ToArray();
            _resourceOwnerServices = services;
            _generateAndSendCode = new GenerateAndSendCodeAction(
                resourceOwnerRepository,
                confirmationCodeStore,
                twoFactorAuthenticationHandler);
            _localOpenIdAuthentication = new LocalOpenIdUserAuthenticationAction(
                authorizationCodeStore,
                services,
                consentRepository,
                tokenStore,
                scopeRepository,
                clientStore,
                jwksStore,
                eventPublisher);
        }

        /// <summary>
        /// Indexes the specified cancellation token.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var authenticatedUser = await SetUser().ConfigureAwait(false);
            if (authenticatedUser?.Identity == null || !authenticatedUser.Identity.IsAuthenticated)
            {
                Request.Query.TryGetValue("ReturnUrl", out var returnUrl);
                var viewModel = new AuthorizeViewModel { ReturnUrl = returnUrl };
                await SetIdProviders(viewModel).ConfigureAwait(false);
                return Ok(viewModel);
            }

            return RedirectToAction("Index", "User");
        }

        /// <summary>
        /// Handles the local login request.
        /// </summary>
        /// <param name="authorizeViewModel">The authorize view model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">authorizeViewModel</exception>
        /// <exception cref="SimpleAuthException">The resource owner credentials are not correct</exception>
        /// <exception cref="Exception">Two factor authenticator is not properly configured</exception>
        [HttpPost]
        public async Task<IActionResult> LocalLogin(
            [FromForm] LocalAuthenticationViewModel authorizeViewModel,
            CancellationToken cancellationToken)
        {
            var authenticatedUser = await SetUser().ConfigureAwait(false);
            if (authenticatedUser?.Identity != null && authenticatedUser.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "User");
            }

            if (authorizeViewModel == null)
            {
                BadRequest();
            }

            if (!ModelState.IsValid)
            {
                var viewModel = new AuthorizeViewModel();
                await SetIdProviders(viewModel).ConfigureAwait(false);
                return View("Index", viewModel);
            }

            try
            {
                var resourceOwner = await _resourceOwnerServices.Authenticate(
                        authorizeViewModel.Login,
                        authorizeViewModel.Password,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (resourceOwner == null)
                {
                    throw new SimpleAuthException(
                        ErrorCodes.InvalidRequest,
                        "The resource owner credentials are not correct");
                }

                resourceOwner.Claims = resourceOwner.Claims.Add(
                    new Claim(
                        ClaimTypes.AuthenticationInstant,
                        DateTimeOffset.UtcNow.ConvertToUnixTimestamp().ToString(CultureInfo.InvariantCulture),
                        ClaimValueTypes.Integer));
                var subject = resourceOwner.Claims.First(c => c.Type == OpenIdClaimTypes.Subject).Value;
                if (string.IsNullOrWhiteSpace(resourceOwner.TwoFactorAuthentication))
                {
                    await SetLocalCookie(resourceOwner.Claims, Id.Create()).ConfigureAwait(false);
                    return !string.IsNullOrWhiteSpace(authorizeViewModel.ReturnUrl)
                        ? (IActionResult)Redirect(authorizeViewModel.ReturnUrl)
                        : RedirectToAction("Index", "User");
                }

                // 2.1 Store temporary information in cookie
                await SetTwoFactorCookie(resourceOwner.Claims).ConfigureAwait(false);
                // 2.2. Send confirmation code
                try
                {
                    await _generateAndSendCode.Send(subject, cancellationToken).ConfigureAwait(false);
                    return RedirectToAction("SendCode");
                }
                catch (ClaimRequiredException cre)
                {
                    await _eventPublisher.Publish(
                            new SimpleAuthError(
                                Id.Create(),
                                cre.Code,
                                cre.Message,
                                string.Empty,
                                DateTimeOffset.UtcNow))
                        .ConfigureAwait(false);
                    return RedirectToAction("SendCode");
                }
                catch (Exception ex)
                {
                    await _eventPublisher.Publish(
                            new SimpleAuthError(
                                Id.Create(),
                                "misconfigured_2fa",
                                ex.Message,
                                string.Empty,
                                DateTimeOffset.UtcNow))
                        .ConfigureAwait(false);
                    throw new Exception("Two factor authenticator is not properly configured", ex);
                }
            }
            catch (Exception exception)
            {
                await _eventPublisher.Publish(
                        new SimpleAuthError(
                            Id.Create(),
                            "invalid_credentials",
                            exception.Message,
                            string.Empty,
                            DateTimeOffset.UtcNow))
                    .ConfigureAwait(false);
                ModelState.AddModelError("invalid_credentials", exception.Message);
                var viewModel = new AuthorizeViewModel();
                await SetIdProviders(viewModel).ConfigureAwait(false);
                return View("Index", viewModel);
            }
        }

        /// <summary>
        /// Handles the local open id login.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// viewModel
        /// or
        /// Code
        /// </exception>
        [HttpPost]
        public async Task<IActionResult> LocalLoginOpenId(
            OpenidLocalAuthenticationViewModel viewModel,
            CancellationToken cancellationToken)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            if (string.IsNullOrWhiteSpace(viewModel.Code))
            {
                throw new ArgumentNullException(nameof(viewModel.Code));
            }

            await SetUser().ConfigureAwait(false);
            try
            {
                // 1. Decrypt the request
                var request = DataProtector.Unprotect<AuthorizationRequest>(viewModel.Code);

                // 3. Check the state of the view model
                if (!ModelState.IsValid)
                {
                    await SetIdProviders(viewModel).ConfigureAwait(false);
                    return View("OpenId", viewModel);
                }

                // 4. Local authentication
                var issuerName = Request.GetAbsoluteUriWithVirtualPath();

                var actionResult = await _localOpenIdAuthentication.Execute(
                        new LocalAuthenticationParameter { UserName = viewModel.Login, Password = viewModel.Password },
                        request.ToParameter(),
                        viewModel.Code,
                        issuerName,
                        cancellationToken)
                    .ConfigureAwait(false);
                var subject = actionResult.Claims.First(c => c.Type == OpenIdClaimTypes.Subject).Value;

                // 5. Two factor authentication.
                if (!string.IsNullOrWhiteSpace(actionResult.TwoFactor))
                {
                    try
                    {
                        await SetTwoFactorCookie(actionResult.Claims).ConfigureAwait(false);
                        await _generateAndSendCode.Send(subject, cancellationToken).ConfigureAwait(false);
                        return RedirectToAction("SendCode", new { code = viewModel.Code });
                    }
                    catch (ClaimRequiredException cre)
                    {
                        await _eventPublisher.Publish(
                                new SimpleAuthError(Id.Create(), cre.Code, cre.Message, string.Empty, DateTimeOffset.UtcNow))
                            .ConfigureAwait(false);
                        return RedirectToAction("SendCode", new { code = viewModel.Code });
                    }
                    catch (Exception ex)
                    {
                        var se = ex as SimpleAuthException;
                        await _eventPublisher.Publish(
                                new SimpleAuthError(Id.Create(), se?.Code, ex.Message, string.Empty, DateTimeOffset.UtcNow))
                            .ConfigureAwait(false);
                        ModelState.AddModelError(
                            "invalid_credentials",
                            "Two factor authenticator is not properly configured");
                    }
                }
                else
                {
                    // 6. Authenticate the user by adding a cookie
                    await SetLocalCookie(actionResult.Claims, request.session_id).ConfigureAwait(false);

                    // 7. Redirect the user agent
                    var result = actionResult.EndpointResult.CreateRedirectionFromActionResult(request);
                    if (result != null)
                    {
                        await LogAuthenticateUser(subject, actionResult.EndpointResult?.Amr).ConfigureAwait(false);
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                var se = ex as SimpleAuthException;
                await _eventPublisher
                    .Publish(new SimpleAuthError(Id.Create(), se?.Code, ex.Message, string.Empty, DateTimeOffset.UtcNow))
                    .ConfigureAwait(false);
                ModelState.AddModelError("invalid_credentials", ex.Message);
            }

            await SetIdProviders(viewModel).ConfigureAwait(false);
            return View("OpenId", viewModel);
        }
    }
}
