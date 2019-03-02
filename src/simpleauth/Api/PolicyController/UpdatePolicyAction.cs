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

namespace SimpleAuth.Api.PolicyController
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using SimpleAuth.Repositories;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.DTOs;
    using SimpleAuth.Shared.Errors;
    using SimpleAuth.Shared.Models;
    using SimpleAuth.Shared.Repositories;

    internal class UpdatePolicyAction
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IResourceSetRepository _resourceSetRepository;

        public UpdatePolicyAction(IPolicyRepository policyRepository, IResourceSetRepository resourceSetRepository)
        {
            _policyRepository = policyRepository;
            _resourceSetRepository = resourceSetRepository;
        }

        public async Task<bool> Execute(PutPolicy updatePolicyParameter, CancellationToken cancellationToken)
        {
            // Check the parameters
            if (updatePolicyParameter == null)
            {
                throw new ArgumentNullException(nameof(updatePolicyParameter));
            }

            if (string.IsNullOrWhiteSpace(updatePolicyParameter.PolicyId))
            {
                throw new SimpleAuthException(
                    ErrorCodes.InvalidRequestCode,
                    string.Format(ErrorDescriptions.TheParameterNeedsToBeSpecified, "id"));
            }

            if (updatePolicyParameter.Rules == null || !updatePolicyParameter.Rules.Any())
            {
                throw new SimpleAuthException(
                    ErrorCodes.InvalidRequestCode,
                    string.Format(
                        ErrorDescriptions.TheParameterNeedsToBeSpecified,
                        UmaConstants.AddPolicyParameterNames.Rules));
            }

            // Check the authorization policy exists.
            Policy policy;
            try
            {
                policy = await _policyRepository.Get(updatePolicyParameter.PolicyId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new SimpleAuthException(
                    ErrorCodes.InternalError,
                    string.Format(
                        ErrorDescriptions.TheAuthorizationPolicyCannotBeRetrieved,
                        updatePolicyParameter.PolicyId),
                    ex);
            }

            if (policy == null)
            {
                return false;
            }

            // Check all the scopes are valid.
            foreach (var resourceSetId in policy.ResourceSetIds)
            {
                var resourceSet = await _resourceSetRepository.Get(resourceSetId).ConfigureAwait(false);
                if (updatePolicyParameter.Rules.Any(
                    r => r.Scopes != null && !r.Scopes.All(s => resourceSet.Scopes.Contains(s))))
                {
                    throw new SimpleAuthException(
                        ErrorCodes.InvalidScope,
                        ErrorDescriptions.OneOrMoreScopesDontBelongToAResourceSet);
                }
            }


            // Update the authorization policy.
            policy.Rules = updatePolicyParameter.Rules.Select(
                    ruleParameter => new PolicyRule
                    {
                        Id = ruleParameter.Id,
                        ClientIdsAllowed = ruleParameter.ClientIdsAllowed,
                        IsResourceOwnerConsentNeeded = ruleParameter.IsResourceOwnerConsentNeeded,
                        Scopes = ruleParameter.Scopes,
                        Script = ruleParameter.Script,
                        Claims = ruleParameter.Claims == null
                            ? Array.Empty<Claim>()
                            : ruleParameter.Claims.Select(c => new Claim(c.Type, c.Value)).ToArray(),
                        OpenIdProvider = ruleParameter.OpenIdProvider
                    })
                .ToArray();

            try
            {
                return await _policyRepository.Update(policy, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new SimpleAuthException(
                    ErrorCodes.InternalError,
                    string.Format(
                        ErrorDescriptions.TheAuthorizationPolicyCannotBeUpdated,
                        updatePolicyParameter.PolicyId),
                    ex);
            }
        }
    }
}
