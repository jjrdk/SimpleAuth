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

namespace SimpleAuth.Common
{
    using Extensions;
    using JwtToken;
    using Parameters;
    using Results;
    using Shared;
    using Shared.Models;
    using SimpleAuth.Shared.Events.Logging;
    using SimpleAuth.Shared.Repositories;
    using SimpleAuth.Shared.Requests;
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using SimpleAuth.Shared.Events.OAuth;

    internal class GenerateAuthorizationResponse
    {
        private readonly IAuthorizationCodeStore _authorizationCodeStore;
        private readonly ITokenStore _tokenStore;
        private readonly JwtGenerator _jwtGenerator;
        private readonly IClientStore _clientStore;
        private readonly IConsentRepository _consentRepository;
        private readonly IJwksStore _jwksStore;
        private readonly IEventPublisher _eventPublisher;

        public GenerateAuthorizationResponse(
            IAuthorizationCodeStore authorizationCodeStore,
            ITokenStore tokenStore,
            IScopeRepository scopeRepository,
            IClientStore clientStore,
            IConsentRepository consentRepository,
            IJwksStore jwksStore,
            IEventPublisher eventPublisher)
        {
            _authorizationCodeStore = authorizationCodeStore;
            _tokenStore = tokenStore;
            _jwtGenerator = new JwtGenerator(clientStore, scopeRepository, jwksStore);
            _eventPublisher = eventPublisher;
            _clientStore = clientStore;
            _consentRepository = consentRepository;
            _jwksStore = jwksStore;
        }

        public async Task Generate(
            EndpointResult endpointResult,
            AuthorizationParameter authorizationParameter,
            ClaimsPrincipal claimsPrincipal,
            Client client,
            string issuerName,
            CancellationToken cancellationToken)
        {
            var newAccessTokenGranted = false;
            var allowedTokenScopes = string.Empty;
            GrantedToken grantedToken = null;
            var newAuthorizationCodeGranted = false;
            AuthorizationCode authorizationCode = null;
            var responses = authorizationParameter.ResponseType.ParseResponseTypes();
            var idTokenPayload = await GenerateIdTokenPayload(
                    claimsPrincipal,
                    authorizationParameter,
                    issuerName,
                    cancellationToken)
                .ConfigureAwait(false);
            var userInformationPayload =
                await GenerateUserInformationPayload(claimsPrincipal, authorizationParameter, cancellationToken)
                    .ConfigureAwait(false);
            if (responses.Contains(ResponseTypeNames.Token))
            {
                // 1. Generate an access token.
                if (!string.IsNullOrWhiteSpace(authorizationParameter.Scope))
                {
                    allowedTokenScopes = string.Join(" ", authorizationParameter.Scope.ParseScopes());
                }

                grantedToken = await _tokenStore.GetValidGrantedToken(
                        allowedTokenScopes,
                        client.ClientId,
                        cancellationToken,
                        idTokenJwsPayload: userInformationPayload,
                        userInfoJwsPayload: idTokenPayload)
                    .ConfigureAwait(false);
                if (grantedToken == null)
                {
                    grantedToken = await client.GenerateToken(
                            _jwksStore,
                            allowedTokenScopes,
                            issuerName,
                            userInformationPayload,
                            idTokenPayload,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    newAccessTokenGranted = true;
                }

                endpointResult.RedirectInstruction.AddParameter(
                    StandardAuthorizationResponseNames.AccessTokenName,
                    grantedToken.AccessToken);
            }

            if (responses.Contains(ResponseTypeNames.Code)) // 2. Generate an authorization code.
            {
                var subject = claimsPrincipal.GetSubject();
                var assignedConsent = await _consentRepository
                    .GetConfirmedConsents(subject, authorizationParameter, cancellationToken)
                    .ConfigureAwait(false);
                if (assignedConsent != null)
                {
                    // Insert a temporary authorization code
                    // It will be used later to retrieve tha id_token or an access token.
                    authorizationCode = new AuthorizationCode
                    {
                        Code = Id.Create(),
                        RedirectUri = authorizationParameter.RedirectUrl,
                        CreateDateTime = DateTime.UtcNow,
                        ClientId = authorizationParameter.ClientId,
                        Scopes = authorizationParameter.Scope,
                        IdTokenPayload = idTokenPayload,
                        UserInfoPayLoad = userInformationPayload
                    };

                    newAuthorizationCodeGranted = true;
                    endpointResult.RedirectInstruction.AddParameter(
                        StandardAuthorizationResponseNames.AuthorizationCodeName,
                        authorizationCode.Code);
                }
            }

            _jwtGenerator.FillInOtherClaimsIdentityTokenPayload(
                idTokenPayload,
                authorizationCode == null ? string.Empty : authorizationCode.Code,
                grantedToken == null ? string.Empty : grantedToken.AccessToken,
                client);

            if (newAccessTokenGranted)
            // 3. Insert the stateful access token into the DB OR insert the access token into the caching.
            {
                await _tokenStore.AddToken(grantedToken, cancellationToken).ConfigureAwait(false);
                await _eventPublisher.Publish(
                        new TokenGranted(
                            Id.Create(),
                            claimsPrincipal.GetSubject(),
                            authorizationParameter.ClientId,
                            allowedTokenScopes,
                            authorizationParameter.ResponseType,
                            DateTime.UtcNow))
                    .ConfigureAwait(false);
            }

            if (newAuthorizationCodeGranted) // 4. Insert the authorization code into the caching.
            {
                if (client.RequirePkce)
                {
                    authorizationCode.CodeChallenge = authorizationParameter.CodeChallenge;
                    authorizationCode.CodeChallengeMethod = authorizationParameter.CodeChallengeMethod;
                }

                await _authorizationCodeStore.Add(authorizationCode, cancellationToken).ConfigureAwait(false);
                await _eventPublisher.Publish(
                        new AuthorizationGranted(
                            Id.Create(),
                            claimsPrincipal.GetSubject(),
                            authorizationParameter.ClientId,
                            DateTime.UtcNow))
                    .ConfigureAwait(false);
            }

            if (responses.Contains(ResponseTypeNames.IdToken))
            {
                var idToken = await _clientStore.GenerateIdToken(
                        authorizationParameter.ClientId,
                        idTokenPayload,
                        _jwksStore,
                        cancellationToken)
                    .ConfigureAwait(false);
                endpointResult.RedirectInstruction.AddParameter(
                    StandardAuthorizationResponseNames.IdTokenName,
                    idToken);
            }

            if (!string.IsNullOrWhiteSpace(authorizationParameter.State))
            {
                endpointResult.RedirectInstruction.AddParameter(
                    StandardAuthorizationResponseNames.StateName,
                    authorizationParameter.State);
            }

            var sessionState = GetSessionState(
                authorizationParameter.ClientId,
                authorizationParameter.OriginUrl,
                authorizationParameter.SessionId);
            if (sessionState != null)
            {
                endpointResult.RedirectInstruction.AddParameter(
                    StandardAuthorizationResponseNames.SessionState,
                    sessionState);
            }

            if (authorizationParameter.ResponseMode == ResponseModes.FormPost)
            {
                endpointResult.Type = ActionResultType.RedirectToAction;
                endpointResult.RedirectInstruction.Action = SimpleAuthEndPoints.FormIndex;
                endpointResult.RedirectInstruction.AddParameter(
                    "redirect_uri",
                    authorizationParameter.RedirectUrl.AbsoluteUri);
            }

            // Set the response mode
            if (endpointResult.Type == ActionResultType.RedirectToCallBackUrl)
            {
                var responseMode = authorizationParameter.ResponseMode;
                if (responseMode == ResponseModes.None)
                {
                    var responseTypes = authorizationParameter.ResponseType.ParseResponseTypes();
                    var authorizationFlow = responseTypes.GetAuthorizationFlow(authorizationParameter.State);
                    responseMode = CoreConstants.MappingAuthorizationFlowAndResponseModes[authorizationFlow];
                }

                endpointResult.RedirectInstruction.ResponseMode = responseMode;
            }
        }

        private string GetSessionState(string clientId, string originUrl, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(originUrl))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return null;
            }

            var salt = Id.Create();
            var s = $"{clientId}{originUrl}{sessionId}{salt}";
            var hex = s.ToSha256Hash();

            return hex.Base64Encode() + "==." + salt;
        }

        private async Task<JwtPayload> GenerateIdTokenPayload(
            ClaimsPrincipal claimsPrincipal,
            AuthorizationParameter authorizationParameter,
            string issuerName,
            CancellationToken cancellationToken)
        {
            JwtPayload jwsPayload;
            if (authorizationParameter.Claims != null
                && authorizationParameter.Claims.IsAnyIdentityTokenClaimParameter())
            {
                jwsPayload = await _jwtGenerator.GenerateFilteredIdTokenPayload(
                        claimsPrincipal,
                        authorizationParameter,
                        authorizationParameter.Claims.IdToken,
                        issuerName,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                jwsPayload = await _jwtGenerator.GenerateIdTokenPayloadForScopes(
                        claimsPrincipal,
                        authorizationParameter,
                        issuerName,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return jwsPayload;
        }

        /// <summary>
        /// Generate the JWS payload for user information endpoint.
        /// If at least one claim is defined then returns the filtered result
        /// Otherwise returns the default payload based on the scopes.
        /// </summary>
        /// <param name="claimsPrincipal"></param>
        /// <param name="authorizationParameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<JwtPayload> GenerateUserInformationPayload(
            ClaimsPrincipal claimsPrincipal,
            AuthorizationParameter authorizationParameter,
            CancellationToken cancellationToken)
        {
            JwtPayload jwsPayload;
            if (authorizationParameter.Claims != null && authorizationParameter.Claims.IsAnyUserInfoClaimParameter())
            {
                jwsPayload = _jwtGenerator.GenerateFilteredUserInfoPayload(
                    authorizationParameter.Claims.UserInfo,
                    claimsPrincipal,
                    authorizationParameter);
            }
            else
            {
                jwsPayload = await _jwtGenerator.GenerateUserInfoPayloadForScope(
                        claimsPrincipal,
                        authorizationParameter,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return jwsPayload;
        }
    }
}
