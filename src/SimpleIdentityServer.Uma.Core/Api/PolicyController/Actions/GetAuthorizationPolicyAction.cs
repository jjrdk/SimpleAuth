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

namespace SimpleAuth.Uma.Api.PolicyController.Actions
{
    using System;
    using System.Threading.Tasks;
    using Errors;
    using Helpers;
    using Models;
    using Repositories;

    internal class GetAuthorizationPolicyAction : IGetAuthorizationPolicyAction
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IRepositoryExceptionHelper _repositoryExceptionHelper;

        public GetAuthorizationPolicyAction(
            IPolicyRepository policyRepository,
            IRepositoryExceptionHelper repositoryExceptionHelper)
        {
            _policyRepository = policyRepository;
            _repositoryExceptionHelper = repositoryExceptionHelper;
        }

        public async Task<Policy> Execute(string policyId)
        {
            if (string.IsNullOrWhiteSpace(policyId))
            {
                throw new ArgumentNullException(nameof(policyId));
            }

            var policy = await _repositoryExceptionHelper.HandleException(
                string.Format(ErrorDescriptions.TheAuthorizationPolicyCannotBeRetrieved, policyId),
                () => _policyRepository.Get(policyId)).ConfigureAwait(false);
            return policy;
        }
    }
}
