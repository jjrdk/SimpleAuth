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
    using System.Threading;
    using System.Threading.Tasks;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Errors;
    using SimpleAuth.Shared.Models;
    using SimpleAuth.Shared.Repositories;

    internal class DeleteResourcePolicyAction
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IResourceSetRepository _resourceSetRepository;

        public DeleteResourcePolicyAction(
            IPolicyRepository policyRepository,
            IResourceSetRepository resourceSetRepository)
        {
            _policyRepository = policyRepository;
            _resourceSetRepository = resourceSetRepository;
        }

        public async Task<bool> Execute(string id, string resourceId, CancellationToken cancellationToken)
        {
            Policy policy;
            try
            {
                policy = await _policyRepository.Get(id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new SimpleAuthException(
                    ErrorCodes.InternalError,
                   string.Format(ErrorDescriptions.TheAuthorizationPolicyCannotBeRetrieved, id),
                    ex);
            }

            if (policy == null)
            {
                return false;
            }

            ResourceSetModel resourceSet;
            try
            {
                resourceSet = await _resourceSetRepository.Get(resourceId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new SimpleAuthException(
                    ErrorCodes.InternalError,
                    string.Format(ErrorDescriptions.TheResourceSetCannotBeRetrieved, resourceId),
                    ex);
            }

            if (resourceSet == null)
            {
                throw new SimpleAuthException(ErrorCodes.InvalidResourceSetId,
                    string.Format(ErrorDescriptions.TheResourceSetDoesntExist, resourceId));
            }

            var result = await _policyRepository.Update(policy, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}
