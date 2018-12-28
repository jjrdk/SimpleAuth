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

namespace SimpleIdentityServer.Uma.Core.UnitTests.Api.PolicyController
{
    using Moq;
    using SimpleAuth.Logging;
    using SimpleAuth.Uma;
    using SimpleAuth.Uma.Api.PolicyController.Actions;
    using SimpleAuth.Uma.Errors;
    using SimpleAuth.Uma.Exceptions;
    using SimpleAuth.Uma.Helpers;
    using SimpleAuth.Uma.Models;
    using SimpleAuth.Uma.Parameters;
    using SimpleAuth.Uma.Repositories;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    public class AddAuthorizationPolicyActionFixture
    {
        private Mock<IPolicyRepository> _policyRepositoryStub;
        private Mock<IResourceSetRepository> _resourceSetRepositoryStub;
        private Mock<IRepositoryExceptionHelper> _repositoryExceptionHelper;
        private Mock<IUmaServerEventSource> _umaServerEventSourceStub;
        private IAddAuthorizationPolicyAction _addAuthorizationPolicyAction;

        [Fact]
        public async Task When_Passing_Null_Parameter_Then_Exception_Is_Thrown()
        {
            InitializeFakeObjects();

            await Assert.ThrowsAsync<ArgumentNullException>(() => _addAuthorizationPolicyAction.Execute(null)).ConfigureAwait(false);
        }

        [Fact]
        public async Task When_Passing_Empty_ResourceSetId_Then_Exception_Is_Thrown()
        {
            InitializeFakeObjects();
            var addPolicyParameter = new AddPolicyParameter();

            var exception = await Assert.ThrowsAsync<BaseUmaException>(() => _addAuthorizationPolicyAction.Execute(addPolicyParameter)).ConfigureAwait(false);
            Assert.NotNull(exception);
            Assert.True(exception.Code == ErrorCodes.InvalidRequestCode);
            Assert.True(exception.Message == string.Format(ErrorDescriptions.TheParameterNeedsToBeSpecified, UmaConstants.AddPolicyParameterNames.ResourceSetIds));
        }

        [Fact]
        public async Task When_Passing_No_Rules_Then_Exception_Is_Thrown()
        {
            InitializeFakeObjects();
            const string resourceSetId = "resource_set_id";
            var addPolicyParameter = new AddPolicyParameter
            {
                ResourceSetIds = new List<string>
                {
                    resourceSetId
                }
            };
            _repositoryExceptionHelper.Setup(r => r.HandleException(string.Format(ErrorDescriptions.TheResourceSetCannotBeRetrieved, resourceSetId), It.IsAny<Func<Task<ResourceSet>>>()))
                .Returns(Task.FromResult((ResourceSet)null));

            var exception = await Assert.ThrowsAsync<BaseUmaException>(() => _addAuthorizationPolicyAction.Execute(addPolicyParameter)).ConfigureAwait(false);
            Assert.NotNull(exception);
            Assert.True(exception.Code == ErrorCodes.InvalidRequestCode);
            Assert.True(exception.Message == string.Format(ErrorDescriptions.TheParameterNeedsToBeSpecified, UmaConstants.AddPolicyParameterNames.Rules));
        }

        [Fact]
        public async Task When_ResourceSetId_Doesnt_Exist_Then_Exception_Is_Thrown()
        {
            InitializeFakeObjects();
            const string resourceSetId = "resource_set_id";
            var addPolicyParameter = new AddPolicyParameter
            {
                ResourceSetIds = new List<string>
                {
                    resourceSetId
                },
                Rules = new List<AddPolicyRuleParameter>
                {
                    new AddPolicyRuleParameter
                    {
                        Scopes = new List<string>
                        {
                            "invalid_scope"
                        },
                        ClientIdsAllowed = new List<string>
                        {
                            "client_id"
                        }
                    }
                }
            };
            _repositoryExceptionHelper.Setup(r => r.HandleException(string.Format(ErrorDescriptions.TheResourceSetCannotBeRetrieved, resourceSetId), It.IsAny<Func<Task<ResourceSet>>>()))
                .Returns(Task.FromResult((ResourceSet)null));

            var exception = await Assert.ThrowsAsync<BaseUmaException>(() => _addAuthorizationPolicyAction.Execute(addPolicyParameter)).ConfigureAwait(false);
            Assert.NotNull(exception);
            Assert.True(exception.Code == ErrorCodes.InvalidResourceSetId);
            Assert.True(exception.Message == string.Format(ErrorDescriptions.TheResourceSetDoesntExist, resourceSetId));
        }

        [Fact]
        public async Task When_Scope_Is_Not_Valid_Then_Exception_Is_Thrown()
        {
            InitializeFakeObjects();
            const string resourceSetId = "resource_set_id";
            var addPolicyParameter = new AddPolicyParameter
            {
                ResourceSetIds = new List<string>
                {
                    resourceSetId
                },
                Rules = new List<AddPolicyRuleParameter>
                {
                    new AddPolicyRuleParameter
                    {
                        Scopes = new List<string>
                        {
                            "invalid_scope"
                        },
                        ClientIdsAllowed = new List<string>
                        {
                            "client_id"
                        }
                    }
                }
            };
            var resourceSet = new ResourceSet
            {
                Scopes = new List<string>
                {
                    "scope"
                }
            };
            _repositoryExceptionHelper.Setup(r => r.HandleException(string.Format(ErrorDescriptions.TheResourceSetCannotBeRetrieved, resourceSetId), It.IsAny<Func<Task<ResourceSet>>>()))
                .Returns(Task.FromResult(resourceSet));

            var exception = await Assert.ThrowsAsync<BaseUmaException>(() => _addAuthorizationPolicyAction.Execute(addPolicyParameter)).ConfigureAwait(false);
            Assert.NotNull(exception);
            Assert.True(exception.Code == ErrorCodes.InvalidScope);
            Assert.True(exception.Message == ErrorDescriptions.OneOrMoreScopesDontBelongToAResourceSet);
        }

        [Fact]
        public async Task When_Adding_AuthorizationPolicy_Then_Id_Is_Returned()
        {
            InitializeFakeObjects();
            const string resourceSetId = "resource_set_id";
            var addPolicyParameter = new AddPolicyParameter
            {
                ResourceSetIds = new List<string>
                {
                    resourceSetId
                },
                Rules = new List<AddPolicyRuleParameter>
                {
                    new AddPolicyRuleParameter
                    {
                        Scopes = new List<string>
                        {
                            "scope"
                        },
                        ClientIdsAllowed = new List<string>
                        {
                            "client_id"
                        },
                        Claims = new List<AddClaimParameter>
                        {
                            new AddClaimParameter
                            {
                                Type = "type",
                                Value = "value"
                            }
                        }
                    }
                }

            };
            var resourceSet = new ResourceSet
            {
                Scopes = new List<string>
                {
                    "scope"
                }
            };
            _repositoryExceptionHelper.Setup(r => r.HandleException(string.Format(ErrorDescriptions.TheResourceSetCannotBeRetrieved, resourceSetId), It.IsAny<Func<Task<ResourceSet>>>()))
                .Returns(Task.FromResult(resourceSet));

            var result = await _addAuthorizationPolicyAction.Execute(addPolicyParameter).ConfigureAwait(false);

            Assert.NotNull(result);
        }

        private void InitializeFakeObjects()
        {
            _policyRepositoryStub = new Mock<IPolicyRepository>();
            _resourceSetRepositoryStub = new Mock<IResourceSetRepository>();
            _repositoryExceptionHelper = new Mock<IRepositoryExceptionHelper>();
            _umaServerEventSourceStub = new Mock<IUmaServerEventSource>();
            _addAuthorizationPolicyAction = new AddAuthorizationPolicyAction(
                _policyRepositoryStub.Object,
                _resourceSetRepositoryStub.Object,
                _repositoryExceptionHelper.Object,
                _umaServerEventSourceStub.Object);
        }
    }
}
