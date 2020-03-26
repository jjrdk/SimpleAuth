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

namespace SimpleAuth.Policies
{
    using Shared.Models;
    using Shared.Repositories;
    using Shared.Responses;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    internal class DefaultAuthorizationPolicy : IAuthorizationPolicy
    {
        private readonly IClientStore _clientStore;

        public DefaultAuthorizationPolicy(IClientStore clientStore)
        {
            _clientStore = clientStore;
        }

        public async Task<AuthorizationPolicyResult> Execute(
            TicketLineParameter ticketLineParameter,
            string claimTokenFormat,
            Claim[] claims,
            CancellationToken cancellationToken,
            params PolicyRule[] authorizationPolicy)
        {
            if (authorizationPolicy == null)
            {
                return new AuthorizationPolicyResult(AuthorizationPolicyResultKind.NotAuthorized);
            }

            AuthorizationPolicyResult result = null;
            foreach (var rule in authorizationPolicy)
            {
                result = await ExecuteAuthorizationPolicyRule(
                        ticketLineParameter,
                        rule,
                        claimTokenFormat,
                        claims,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (result.Result == AuthorizationPolicyResultKind.Authorized)
                {
                    return result;
                }
            }

            return result;
        }

        private async Task<AuthorizationPolicyResult> ExecuteAuthorizationPolicyRule(
            TicketLineParameter ticketLineParameter,
            PolicyRule authorizationPolicy,
            string claimTokenFormat,
            Claim[] claims,
            CancellationToken cancellationToken)
        {
            // 1. Check can access to the scope
            if (ticketLineParameter.Scopes.Any(s => !authorizationPolicy.Scopes.Contains(s)))
            {
                return new AuthorizationPolicyResult(AuthorizationPolicyResultKind.NotAuthorized);
            }

            // 2. Check clients are correct
            var clientAuthorizationResult =
                authorizationPolicy.ClientIdsAllowed?.Contains(ticketLineParameter.ClientId);
            if (clientAuthorizationResult != true)
            {
                return new AuthorizationPolicyResult(AuthorizationPolicyResultKind.NotAuthorized);
            }

            // 4. Check the resource owner consent is needed
            if (authorizationPolicy.IsResourceOwnerConsentNeeded && !ticketLineParameter.IsAuthorizedByRo)
            {
                return new AuthorizationPolicyResult(AuthorizationPolicyResultKind.RequestSubmitted);
            }

            // 3. Check claims are correct
            var claimAuthorizationResult = await CheckClaims(
                    ticketLineParameter.ClientId,
                    authorizationPolicy,
                    claimTokenFormat,
                    claims,
                    cancellationToken)
                .ConfigureAwait(false);
            if (claimAuthorizationResult != null
                && claimAuthorizationResult.Result != AuthorizationPolicyResultKind.Authorized)
            {
                return claimAuthorizationResult;
            }

            return new AuthorizationPolicyResult(AuthorizationPolicyResultKind.Authorized);
        }

        private static AuthorizationPolicyResult GetNeedInfoResult(ClaimData[] claims, string openidConfigurationUrl)
        {
            var requestingPartyClaims = new Dictionary<string, object>();
            var requiredClaims = claims.Select(
                    claim => new Dictionary<string, string>
                    {
                        {"name", claim.Type},
                        {"friendly_name", claim.Type},
                        {"issuer", openidConfigurationUrl}
                    })
                .ToList();

            requestingPartyClaims.Add("required_claims", requiredClaims);
            requestingPartyClaims.Add("redirect_user", false);
            return new AuthorizationPolicyResult(
                AuthorizationPolicyResultKind.NeedInfo,
                new Dictionary<string, object>
                {
                    {"requesting_party_claims", requestingPartyClaims}
                });
        }

        private async Task<AuthorizationPolicyResult> CheckClaims(
            string clientId,
            PolicyRule authorizationPolicy,
            string claimTokenFormat,
            Claim[] claims,
            CancellationToken cancellationToken)
        {
            if (authorizationPolicy.Claims == null || !authorizationPolicy.Claims.Any())
            {
                return new AuthorizationPolicyResult(AuthorizationPolicyResultKind.Authorized);
            }

            if (claimTokenFormat != UmaConstants.IdTokenType)
            {
                return GetNeedInfoResult(authorizationPolicy.Claims, authorizationPolicy.OpenIdProvider);
            }

            var client = await _clientStore.GetById(clientId, cancellationToken).ConfigureAwait(false);

            if (claims == null)
            {
                return new AuthorizationPolicyResult(AuthorizationPolicyResultKind.NotAuthorized);
            }

            foreach (var claim in authorizationPolicy.Claims)
            {
                var payload = claims.FirstOrDefault(j => j.Type == claim.Type);
                if (payload.Equals(default(KeyValuePair<string, object>)))
                {
                    return new AuthorizationPolicyResult(AuthorizationPolicyResultKind.NotAuthorized);
                }

                if (payload.ValueType == JsonClaimValueTypes.JsonArray) // is IEnumerable<string> strings)
                {
                    var strings = JsonConvert.DeserializeObject<object[]>(payload.Value);
                    if (!strings.Any(s => string.Equals(s, claim.Value)))
                    {
                        return new AuthorizationPolicyResult(AuthorizationPolicyResultKind.NotAuthorized);
                    }
                }

                if (payload.Value != claim.Value)
                {
                    return new AuthorizationPolicyResult(AuthorizationPolicyResultKind.NotAuthorized);
                }
            }

            return new AuthorizationPolicyResult(AuthorizationPolicyResultKind.Authorized);
        }
    }
}
