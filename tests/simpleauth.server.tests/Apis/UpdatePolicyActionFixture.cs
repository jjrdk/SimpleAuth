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

namespace SimpleAuth.Server.Tests.Apis
{
    using Moq;
    using SimpleAuth.Api.PolicyController;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.DTOs;
    using SimpleAuth.Shared.Models;
    using SimpleAuth.Shared.Repositories;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class UpdatePolicyActionFixture
    {
        private Mock<IPolicyRepository> _policyRepositoryStub;
        private Mock<IResourceSetRepository> _resourceSetRepositoryStub;
        private UpdatePolicyAction _updatePolicyAction;

        [Fact]
        public async Task When_Passing_Null_Parameter_Then_Exception_Is_Thrown()
        {
            InitializeFakeObjects();

            await Assert
                .ThrowsAsync<NullReferenceException>(() => _updatePolicyAction.Execute(null, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task WhenIdIsNotPassedThenReturnsFalse()
        {
            var updatePolicyParameter = new PolicyData();
            InitializeFakeObjects();

            var result = await _updatePolicyAction.Execute(updatePolicyParameter, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.False(result);
        }

        [Fact]
        public async Task WhenRulesAreNotPassedThenReturnsFalse()
        {
            var updatePolicyParameter = new PolicyData { PolicyId = "not_valid_policy_id" };
            InitializeFakeObjects();

            var results = await _updatePolicyAction.Execute(updatePolicyParameter, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.False(results);
        }

        [Fact]
        public async Task When_Authorization_Policy_Does_Not_Exist_Then_False_Is_Returned()
        {
            var updatePolicyParameter = new PolicyData
            {
                PolicyId = "not_valid_policy_id",
                Rules = new[] { new PolicyRuleData() }
            };
            InitializeFakeObjects();

            var result = await _updatePolicyAction.Execute(updatePolicyParameter, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.False(result);
        }

        [Fact]
        public async Task When_Scope_Is_Not_Valid_Then_Exception_Is_Thrown()
        {
            var updatePolicyParameter = new PolicyData
            {
                PolicyId = "policy_id",
                Rules = new[] { new PolicyRuleData { Scopes = new[] { "invalid_scope" } } }
            };
            var policy = new Policy();
            InitializeFakeObjects(policy);

            _resourceSetRepositoryStub.Setup(r => r.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceSetModel { Scopes = new[] { "scope" } });

            var result = await Assert
                .ThrowsAsync<SimpleAuthException>(
                    () => _updatePolicyAction.Execute(updatePolicyParameter, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.Equal("invalid_scope", result.Code);
            Assert.Equal("one or more scopes don't belong to a resource set", result.Message);
        }

        [Fact]
        public async Task When_Authorization_Policy_Is_Updated_Then_True_Is_Returned()
        {
            var updatePolicyParameter = new PolicyData
            {
                PolicyId = "valid_policy_id",
                Rules = new[]
                {
                    new PolicyRuleData
                    {
                        Claims = new[] {new ClaimData {Type = "type", Value = "value"}},
                        Scopes = new[] {"scope"}
                    }
                }
            };
            var policy = new Policy { };
            InitializeFakeObjects(policy);

            _resourceSetRepositoryStub.Setup(r => r.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceSetModel { Scopes = new[] { "scope" } });

            var result = await _updatePolicyAction.Execute(updatePolicyParameter, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.True(result);
        }

        private void InitializeFakeObjects(Policy policy = null)
        {
            _policyRepositoryStub = new Mock<IPolicyRepository>();
            _policyRepositoryStub.Setup(x => x.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(policy);
            _policyRepositoryStub.Setup(x => x.Update(It.IsAny<Policy>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _resourceSetRepositoryStub = new Mock<IResourceSetRepository>();
            _updatePolicyAction = new UpdatePolicyAction(
                _policyRepositoryStub.Object);
        }
    }
}
