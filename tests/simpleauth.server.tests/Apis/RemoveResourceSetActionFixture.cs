﻿namespace SimpleAuth.Server.Tests.Apis
{
    using System;
    using System.Threading.Tasks;
    using Moq;
    using SimpleAuth.Api.ResourceSetController;
    using SimpleAuth.Repositories;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Errors;
    using SimpleAuth.Shared.Models;
    using Xunit;

    public class RemoveResourceSetActionFixture
    {
        private readonly Mock<IResourceSetRepository> _resourceSetRepositoryStub;
        private readonly DeleteResourceSetAction _deleteResourceSetAction;

        public RemoveResourceSetActionFixture()
        {
            _resourceSetRepositoryStub = new Mock<IResourceSetRepository>();
            _deleteResourceSetAction = new DeleteResourceSetAction(_resourceSetRepositoryStub.Object);
        }

        [Fact]
        public async Task When_Passing_Null_Parameter_Then_Exception_Is_Thrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _deleteResourceSetAction.Execute(null))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task When_ResourceSet_Does_Not_Exist_Then_False_Is_Returned()
        {
            const string resourceSetId = "resourceSetId";
            _resourceSetRepositoryStub.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(() => Task.FromResult((ResourceSet) null));

            var result = await _deleteResourceSetAction.Execute(resourceSetId).ConfigureAwait(false);

            Assert.False(result);
        }

        [Fact]
        public async Task When_ResourceSet_Cannot_Be_Updated_Then_Exception_Is_Thrown()
        {
            const string resourceSetId = "resourceSetId";
            _resourceSetRepositoryStub.Setup(r => r.Get(It.IsAny<string>()))
                .ReturnsAsync(new ResourceSet());
            _resourceSetRepositoryStub.Setup(r => r.Delete(It.IsAny<string>())).ReturnsAsync(false);

            var exception = await Assert
                .ThrowsAsync<SimpleAuthException>(() => _deleteResourceSetAction.Execute(resourceSetId))
                .ConfigureAwait(false);
            Assert.Equal(ErrorCodes.InternalError, exception.Code);
            Assert.Equal(
                string.Format(ErrorDescriptions.TheResourceSetCannotBeRemoved, resourceSetId),
                exception.Message);
        }

        [Fact]
        public async Task When_ResourceSet_Is_Removed_Then_True_Is_Returned()
        {
            const string resourceSetId = "resourceSetId";
            _resourceSetRepositoryStub.Setup(r => r.Get(It.IsAny<string>()))
                .ReturnsAsync(new ResourceSet());
            _resourceSetRepositoryStub.Setup(r => r.Delete(It.IsAny<string>())).ReturnsAsync(true);

            var result = await _deleteResourceSetAction.Execute(resourceSetId).ConfigureAwait(false);

            Assert.True(result);
        }
    }
}
