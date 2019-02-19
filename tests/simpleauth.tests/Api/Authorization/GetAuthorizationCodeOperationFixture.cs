﻿namespace SimpleAuth.Tests.Api.Authorization
{
    using Exceptions;
    using Moq;
    using Parameters;
    using Shared;
    using Shared.Models;
    using Shared.Repositories;
    using SimpleAuth.Api.Authorization;
    using SimpleAuth.MiddleWare;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using SimpleAuth.Shared.Errors;
    using Xunit;
    using Client = Shared.Models.Client;

    public sealed class GetAuthorizationCodeOperationFixture
    {
        private const string HttpsLocalhost = "https://localhost";
        private readonly GetAuthorizationCodeOperation _getAuthorizationCodeOperation;

        public GetAuthorizationCodeOperationFixture()
        {
            _getAuthorizationCodeOperation = new GetAuthorizationCodeOperation(
                new Mock<IAuthorizationCodeStore>().Object,
                new Mock<ITokenStore>().Object,
                new Mock<IScopeRepository>().Object,
                new Mock<IClientStore>().Object,
                new Mock<IConsentRepository>().Object,
                new NoOpPublisher());
        }

        [Fact]
        public async Task When_Passing_Null_Parameters_Then_Exceptions_Are_Thrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _getAuthorizationCodeOperation.Execute(null, null, null, null, CancellationToken.None)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => _getAuthorizationCodeOperation.Execute(new AuthorizationParameter(), null, null, null, CancellationToken.None)).ConfigureAwait(false);
        }

        [Fact]
        public async Task When_The_Client_Grant_Type_Is_Not_Supported_Then_Exception_Is_Thrown()
        {
            const string clientId = "clientId";
            const string scope = "scope";
            var authorizationParameter = new AuthorizationParameter
            {
                ResponseType = ResponseTypeNames.Code,
                RedirectUrl = new Uri(HttpsLocalhost),
                ClientId = clientId,
                Scope = scope,
                Claims = null
            };
            //_clientValidatorFake.Setup(c => c.CheckGrantTypes(It.IsAny<Client>(), It.IsAny<GrantType[]>()))
            //    .Returns(false);
            var client = new Client
            {
                GrantTypes = new[] { GrantTypes.ClientCredentials },
                AllowedScopes = new[] { new Scope { Name = scope } },
                RedirectionUrls = new[] { new Uri(HttpsLocalhost), }
            };
            var exception = await Assert.ThrowsAsync<SimpleAuthExceptionWithState>(() =>
                    _getAuthorizationCodeOperation.Execute(authorizationParameter, null, client, null, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.NotNull(exception);
            Assert.Equal(ErrorCodes.InvalidRequestCode, exception.Code);
            Assert.Equal(string.Format(ErrorDescriptions.TheClientDoesntSupportTheGrantType,
                    clientId,
                    "authorization_code"),
                exception.Message);
        }

        [Fact]
        public async Task When_Passing_Valid_Request_Then_ReturnsRedirectInstruction()
        {
            const string clientId = "clientId";
            const string scope = "scope";

            var client = new Client
            {
                ResponseTypes = new[] { ResponseTypeNames.Code },
                AllowedScopes = new[] { new Scope { Name = scope } },
                RedirectionUrls = new[] { new Uri(HttpsLocalhost), }
            };
            var authorizationParameter = new AuthorizationParameter
            {
                ResponseType = ResponseTypeNames.Code,
                RedirectUrl = new Uri(HttpsLocalhost),
                ClientId = clientId,
                Scope = scope,
                Claims = null
            };

            var result = await _getAuthorizationCodeOperation.Execute(authorizationParameter, null, client, null, CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(result.RedirectInstruction);
        }
    }
}
