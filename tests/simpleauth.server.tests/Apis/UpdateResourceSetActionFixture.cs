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
    using System.Threading.Tasks;
    using Moq;
    using SimpleAuth.Api.ResourceSetController;
    using SimpleAuth.Repositories;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.DTOs;
    using SimpleAuth.Shared.Errors;
    using SimpleAuth.Shared.Models;
    using Xunit;

    public class UpdateResourceSetActionFixture
    {
        private readonly Mock<IResourceSetRepository> _resourceSetRepositoryStub;
        private readonly UpdateResourceSetAction _updateResourceSetAction;

        public UpdateResourceSetActionFixture()
        {
            _resourceSetRepositoryStub = new Mock<IResourceSetRepository>();
            _resourceSetRepositoryStub.Setup(x => x.Update(It.IsAny<ResourceSet>())).ReturnsAsync(true);
            _updateResourceSetAction = new UpdateResourceSetAction(_resourceSetRepositoryStub.Object);
        }

        [Fact]
        public async Task When_Passing_No_Parameter_Then_Exception_Is_Thrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _updateResourceSetAction.Execute(null))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task When_ResourceSet_Cannot_Be_Updated_Then_Exception_Is_Thrown()
        {
            const string id = "id";
            var udpateResourceSetParameter = new PutResourceSet
            {
                Id = id, Name = "blah", Scopes = new [] {"scope"}
            };
            var resourceSet = new ResourceSet {Id = id};
            _resourceSetRepositoryStub.Setup(r => r.Get(It.IsAny<string>())).ReturnsAsync(resourceSet);
            _resourceSetRepositoryStub.Setup(r => r.Update(It.IsAny<ResourceSet>()))
                .Returns(() => Task.FromResult(false));

            var exception = await Assert
                .ThrowsAsync<SimpleAuthException>(() => _updateResourceSetAction.Execute(udpateResourceSetParameter))
                .ConfigureAwait(false);
            Assert.Equal(ErrorCodes.InternalError, exception.Code);
            Assert.Equal(
                string.Format(ErrorDescriptions.TheResourceSetCannotBeUpdated, udpateResourceSetParameter.Id),
                exception.Message);
        }

        [Fact]
        public async Task When_A_ResourceSet_Is_Updated_Then_True_Is_Returned()
        {
            const string id = "id";
            var udpateResourceSetParameter = new PutResourceSet
            {
                Id = id, Name = "blah", Scopes = new [] {"scope"}
            };
            var resourceSet = new ResourceSet {Id = id};
            _resourceSetRepositoryStub.Setup(r => r.Get(It.IsAny<string>())).ReturnsAsync(resourceSet);
            _resourceSetRepositoryStub.Setup(r => r.Update(It.IsAny<ResourceSet>())).ReturnsAsync(true);

            var result = await _updateResourceSetAction.Execute(udpateResourceSetParameter).ConfigureAwait(false);

            Assert.True(result);
        }
    }
}
