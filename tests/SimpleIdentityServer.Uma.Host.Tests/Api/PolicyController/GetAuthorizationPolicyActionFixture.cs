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

namespace SimpleAuth.Uma.Tests.Api.PolicyController
{
    using System;
    using System.Threading.Tasks;
    using Errors;
    using Helpers;
    using Models;
    using Moq;
    using Repositories;
    using Uma.Api.PolicyController.Actions;
    using Xunit;

    public class GetAuthorizationPolicyActionFixture
    {
        private Mock<IPolicyRepository> _policyRepositoryStub;
        private Mock<IRepositoryExceptionHelper> _repositoryExceptionHelperStub;
        private IGetAuthorizationPolicyAction _getAuthorizationPolicyAction;

        [Fact]
        public async Task When_Passing_Empty_Parameter_Then_Exception_Is_Thrown()
        {
            InitializeFakeObjects();

            await Assert.ThrowsAsync<ArgumentNullException>(() => _getAuthorizationPolicyAction.Execute(null)).ConfigureAwait(false);
        }

        [Fact]
        public async Task When_Getting_Policy_Then_Policy_Is_Returned()
        {
            const string policyId = "policy_id";
            var policy = new Policy
            {
                Id = policyId
            };
            InitializeFakeObjects();
            _repositoryExceptionHelperStub.Setup(r => r.HandleException(
                string.Format(ErrorDescriptions.TheAuthorizationPolicyCannotBeRetrieved, policyId),
                It.IsAny<Func<Task<Policy>>>()))
                .Returns(Task.FromResult(policy));

            var result = await _getAuthorizationPolicyAction.Execute(policyId).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.True(result.Id == policyId);
        }

        private void InitializeFakeObjects()
        {
            _policyRepositoryStub = new Mock<IPolicyRepository>();
            _repositoryExceptionHelperStub = new Mock<IRepositoryExceptionHelper>();
            _getAuthorizationPolicyAction = new GetAuthorizationPolicyAction(
                _policyRepositoryStub.Object,
                _repositoryExceptionHelperStub.Object);
        }
    }
}
