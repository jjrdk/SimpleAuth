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

namespace SimpleAuth.Uma.Tests.Api.ResourceSetController.Actions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Errors;
    using Exceptions;
    using Moq;
    using Repositories;
    using SimpleAuth.Api.ResourceSetController.Actions;
    using SimpleAuth.Shared.Models;
    using Xunit;

    public class GetAllResourceSetActionFixture
    {
        private Mock<IResourceSetRepository> _resourceSetRepositoryStub;
        private IGetAllResourceSetAction _getAllResourceSetAction;

        [Fact]
        public async Task When_Error_Occured_While_Trying_To_Retrieve_ResourceSet_Then_Exception_Is_Thrown()
        {
            InitializeFakeObject();
            _resourceSetRepositoryStub.Setup(r => r.GetAll())
                .Returns(() => Task.FromResult((ICollection<ResourceSet>) null));

            var exception = await Assert.ThrowsAsync<SimpleAuthException>(() => _getAllResourceSetAction.Execute())
                .ConfigureAwait(false);
            Assert.NotNull(exception);
            Assert.True(exception.Code == ErrorCodes.InternalError);
            Assert.True(exception.Message == ErrorDescriptions.TheResourceSetsCannotBeRetrieved);
        }

        [Fact]
        public async Task When_ResourceSets_Are_Retrieved_Then_Ids_Are_Returned()
        {
            const string id = "id";
            ICollection<ResourceSet> resourceSets = new List<ResourceSet>
            {
                new ResourceSet
                {
                    Id = id
                }
            };
            InitializeFakeObject();
            _resourceSetRepositoryStub.Setup(r => r.GetAll())
                .Returns(Task.FromResult(resourceSets));

            var result = await _getAllResourceSetAction.Execute().ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.True(result.Count() == 1);
            Assert.True(result.First() == id);
        }

        private void InitializeFakeObject()
        {
            _resourceSetRepositoryStub = new Mock<IResourceSetRepository>();
            _getAllResourceSetAction = new GetAllResourceSetAction(_resourceSetRepositoryStub.Object);
        }
    }
}
