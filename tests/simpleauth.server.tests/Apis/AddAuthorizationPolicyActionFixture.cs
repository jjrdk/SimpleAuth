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
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using SimpleAuth.Api.PolicyController;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.DTOs;
    using SimpleAuth.Shared.Errors;
    using SimpleAuth.Shared.Models;
    using SimpleAuth.Shared.Repositories;
    using Xunit;

    public class AddAuthorizationPolicyActionFixture
    {
        private Mock<IPolicyRepository> _policyRepositoryStub;
        private Mock<IResourceSetRepository> _resourceSetRepositoryStub;
        private AddAuthorizationPolicyAction _addAuthorizationPolicyAction;

        [Fact]
        public async Task When_Passing_Null_Parameter_Then_Exception_Is_Thrown()
        {
            InitializeFakeObjects();

            await Assert
                .ThrowsAsync<NullReferenceException>(
                    () => _addAuthorizationPolicyAction.Execute(null, null, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task When_Passing_No_Rules_Then_Exception_Is_Thrown()
        {
            InitializeFakeObjects();
            var addPolicyParameter = new PolicyData();

            var exception = await Assert.ThrowsAsync<SimpleAuthException>(
                    () => _addAuthorizationPolicyAction.Execute("owner", addPolicyParameter, CancellationToken.None))
                .ConfigureAwait(false);
            Assert.Equal(ErrorCodes.InvalidRequest, exception.Code);
            Assert.True(
                exception.Message
                == string.Format(
                    ErrorDescriptions.TheParameterNeedsToBeSpecified,
                    UmaConstants.AddPolicyParameterNames.Rules));
        }

        [Fact]
        public async Task When_Adding_AuthorizationPolicy_Then_Id_Is_Returned()
        {
            var addPolicyParameter = new PolicyData
            {
                Rules = new[]
                {
                    new PolicyRuleData
                    {
                        Scopes = new[] {"scope"},
                        ClientIdsAllowed = new[] {"client_id"},
                        Claims = new[] {new ClaimData {Type = "type", Value = "value"}}
                    }
                }
            };
            var resourceSet = new ResourceSetModel { Scopes = new[] { "scope" } };

            InitializeFakeObjects(resourceSet);

            var result = await _addAuthorizationPolicyAction
                .Execute("owner", addPolicyParameter, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.NotNull(result);
        }

        private void InitializeFakeObjects(ResourceSetModel resourceSet = null)
        {
            _policyRepositoryStub = new Mock<IPolicyRepository>();
            _policyRepositoryStub.Setup(x => x.Add(It.IsAny<Policy>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _resourceSetRepositoryStub = new Mock<IResourceSetRepository>();
            _resourceSetRepositoryStub.Setup(x => x.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(resourceSet);
            _resourceSetRepositoryStub.Setup(x => x.Get(It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(new[] { resourceSet });

            _addAuthorizationPolicyAction = new AddAuthorizationPolicyAction(
                _policyRepositoryStub.Object);
        }
    }
}
