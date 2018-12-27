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


using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SimpleIdentityServer.Manager.Core.Tests.Api.Scopes.Actions
{
    using SimpleAuth.Api.Scopes.Actions;
    using SimpleAuth.Errors;
    using SimpleAuth.Exceptions;
    using SimpleAuth.Logging;
    using SimpleAuth.Shared.Models;
    using SimpleAuth.Shared.Repositories;

    public class DeleteScopeOperationFixture
    {
        private Mock<IScopeRepository> _scopeRepositoryStub;
        private Mock<IManagerEventSource> _managerEventSourceStub;
        private IDeleteScopeOperation _deleteScopeOperation;

        [Fact]
        public async Task When_Passing_Null_Parameter_Then_Exception_Is_Thrown()
        {            InitializeFakeObjects();

                        await Assert.ThrowsAsync<ArgumentNullException>(() => _deleteScopeOperation.Execute(null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => _deleteScopeOperation.Execute(string.Empty)).ConfigureAwait(false);
        }

        [Fact]
        public async Task When_Passing_Not_Existing_ScopeName_Then_Exception_Is_Thrown()
        {            const string scopeName = "invalid_scope";
            InitializeFakeObjects();
            _scopeRepositoryStub.Setup(c => c.Get(It.IsAny<string>()))
                .Returns(Task.FromResult((Scope)null));

                        var exception = await Assert.ThrowsAsync<IdentityServerException>(() => _deleteScopeOperation.Execute(scopeName)).ConfigureAwait(false);
            Assert.True(exception.Code == ErrorCodes.InvalidRequestCode);
            Assert.True(exception.Message == string.Format(ErrorDescriptions.TheScopeDoesntExist, scopeName));
        }

        [Fact]
        public async Task When_Deleting_ExistingScope_Then_Operation_Is_Called()
        {            const string scopeName = "client_id";
            var scope = new Scope
            {
                Name = scopeName
            };
            InitializeFakeObjects();
            _scopeRepositoryStub.Setup(c => c.Get(It.IsAny<string>()))
                .Returns(Task.FromResult(scope));

                        await _deleteScopeOperation.Execute(scopeName).ConfigureAwait(false);

                        _scopeRepositoryStub.Verify(c => c.Delete(scope));
        }

        private void InitializeFakeObjects()
        {
            _scopeRepositoryStub = new Mock<IScopeRepository>();
            _managerEventSourceStub = new Mock<IManagerEventSource>();
            _deleteScopeOperation = new DeleteScopeOperation(
                _scopeRepositoryStub.Object,
                _managerEventSourceStub.Object);
        }
    }
}
