﻿using System.Collections.Generic;

namespace SimpleAuth.Tests.Api.Authorization
{
    using Errors;
    using Exceptions;
    using Moq;
    using Parameters;
    using Results;
    using Shared;
    using Shared.Models;
    using Shared.Repositories;
    using SimpleAuth;
    using SimpleAuth.Api.Authorization;
    using SimpleAuth.Common;
    using System;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Xunit;

    //using Client = Shared.Models.Client;

    public sealed class GetAuthorizationCodeAndTokenViaHybridWorkflowOperationFixture
    {
        private Mock<IGenerateAuthorizationResponse> _generateAuthorizationResponseFake;
        private GetAuthorizationCodeAndTokenViaHybridWorkflowOperation _getAuthorizationCodeAndTokenViaHybridWorkflowOperation;
        private Mock<IConsentRepository> _consentRepository;

        public GetAuthorizationCodeAndTokenViaHybridWorkflowOperationFixture()
        {
            InitializeFakeObjects();
        }

        [Fact]
        public async Task When_Passing_Null_Parameters_Then_Exceptions_Are_Thrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                    () => _getAuthorizationCodeAndTokenViaHybridWorkflowOperation.Execute(null, null, null, null))
                .ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(
                    () => _getAuthorizationCodeAndTokenViaHybridWorkflowOperation.Execute(
                        new AuthorizationParameter(),
                        null,
                        null,
                        null))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task When_Nonce_Parameter_Is_Not_Set_Then_Exception_Is_Thrown()
        {
            var authorizationParameter = new AuthorizationParameter { State = "state" };

            var ex = await Assert.ThrowsAsync<SimpleAuthExceptionWithState>(
                    () => _getAuthorizationCodeAndTokenViaHybridWorkflowOperation.Execute(
                        authorizationParameter,
                        null,
                        new Client(),
                        null))
                .ConfigureAwait(false);
            Assert.Equal(ErrorCodes.InvalidRequestCode, ex.Code);
            Assert.Equal(
                string.Format(
                    ErrorDescriptions.MissingParameter,
                    CoreConstants.StandardAuthorizationRequestParameterNames.NonceName),
                ex.Message);
            Assert.Equal(authorizationParameter.State, ex.State);
        }

        [Fact]
        public async Task When_Grant_Type_Is_Not_Supported_Then_Exception_Is_Thrown()
        {
            var redirectUrl = new Uri("https://localhost");
            var authorizationParameter = new AuthorizationParameter
            {
                RedirectUrl = redirectUrl,
                State = "state",
                Nonce = "nonce",
                Scope = "openid",
                ResponseType = ResponseTypeNames.Code,
            };

            //_clientValidatorFake
            //    .Setup(c => c.CheckGrantTypes(It.IsAny<Client>(), It.IsAny<GrantType[]>()))
            //    .Returns(false);

            var client = new Client
            {
                RedirectionUrls = new[] { redirectUrl },
                AllowedScopes = new[] { new Scope { Name = "openid" } },
            };
            var ex = await Assert.ThrowsAsync<SimpleAuthExceptionWithState>(
                    () => _getAuthorizationCodeAndTokenViaHybridWorkflowOperation.Execute(
                        authorizationParameter,
                        null,
                        client,
                        null))
                .ConfigureAwait(false);
            Assert.Equal(ErrorCodes.InvalidRequestCode, ex.Code);
            Assert.Equal(
                string.Format(
                    ErrorDescriptions.TheClientDoesntSupportTheGrantType,
                    authorizationParameter.ClientId,
                    "implicit and authorization_code"),
                ex.Message);
            Assert.Equal(authorizationParameter.State, ex.State);
        }

        [Fact(Skip = "Invalid test")]
        public async Task When_Redirected_To_Callback_And_Resource_Owner_Is_Not_Authenticated_Then_Exception_Is_Thrown()
        {
            var redirectUrl = new Uri("https://localhost");
            var authorizationParameter = new AuthorizationParameter
            {
                Prompt = PromptNames.None,
                ClientId = "test",
                State = "state",
                Nonce = "nonce",
                RedirectUrl = redirectUrl,
                Scope = "openid",
                ResponseType = ResponseTypeNames.IdToken
            };

            //var actionResult = new EndpointResult
            //{
            //    Type = TypeActionResult.RedirectToCallBackUrl
            //};

            //_processAuthorizationRequestFake.Setup(p => p.ProcessAsync(It.IsAny<AuthorizationParameter>(),
            //        It.IsAny<ClaimsPrincipal>(),
            //        It.IsAny<Client>(),
            //        null))
            //    .Returns(Task.FromResult(actionResult));
            //_clientValidatorFake.Setup(c =>
            //        c.CheckGrantTypes(It.IsAny<Client>(), It.IsAny<GrantType[]>()))
            //    .Returns(true);

            var client = new Client
            {
                ClientId = "test",
                GrantTypes = new[] { GrantType.@implicit, GrantType.authorization_code },
                ResponseTypes = new[] { ResponseTypeNames.IdToken },
                AllowedScopes = new[] { new Scope { Name = "openid", IsDisplayedInConsent = true } },
                RedirectionUrls = new[] { redirectUrl }
            };
            var ex = await Assert.ThrowsAsync<SimpleAuthExceptionWithState>(
                    () => _getAuthorizationCodeAndTokenViaHybridWorkflowOperation.Execute(
                        authorizationParameter,
                        null, // new ClaimsPrincipal(new ClaimsIdentity(new Claim[0], "")),
                        client,
                        null))
                .ConfigureAwait(false);
            Assert.Equal(ErrorCodes.InvalidRequestCode, ex.Code);
            Assert.Equal(
                ErrorDescriptions.TheResponseCannotBeGeneratedBecauseResourceOwnerNeedsToBeAuthenticated,
                ex.Message);
            Assert.Equal(authorizationParameter.State, ex.State);
        }

        [Fact]
        public async Task
            When_Resource_Owner_Is_Authenticated_And_Pass_Correct_Authorization_Request_Then_Events_Are_Logged()
        {
            var authorizationParameter = new AuthorizationParameter
            {
                Prompt = PromptNames.None,
                ResponseType = ResponseTypeNames.Code,
                RedirectUrl = new Uri("https://localhost"),
                State = "state",
                ClientId = "client_id",
                Scope = "scope",
                Nonce = "nonce"
            };
            var consent = new Consent
            {
                Client = new Client { ClientId = "client_id", AllowedScopes = { new Scope { Name = "scope" } } }
            };
            consent.GrantedScopes = new List<Scope> { new Scope { Name = "scope" } };
            _consentRepository.Setup(x => x.GetConsentsForGivenUser(It.IsAny<string>()))
                .ReturnsAsync(new[] { consent });
            //var actionResult = new EndpointResult
            //{
            //    Type = TypeActionResult.RedirectToCallBackUrl,
            //    RedirectInstruction = new RedirectInstruction {Action = SimpleAuthEndPoints.ConsentIndex}
            //};

            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.AuthenticationInstant, "1"), new Claim("sub", "test") },
                "Cookies");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var client = new Client
            {
                GrantTypes = new[] { GrantType.@implicit, GrantType.authorization_code },
                ResponseTypes = ResponseTypeNames.All,
                RedirectionUrls = new[] { new Uri("https://localhost"), },
                AllowedScopes = new[] { new Scope { Name = "scope" } }
            };

            var endpointResult = await _getAuthorizationCodeAndTokenViaHybridWorkflowOperation
                    .Execute(authorizationParameter, claimsPrincipal, client, null)
                    .ConfigureAwait(false);
            _generateAuthorizationResponseFake.Verify(
                g => g.Generate(
                    It.IsAny<EndpointResult>(),
                    authorizationParameter,
                    claimsPrincipal,
                    It.IsAny<Client>(),
                    It.IsAny<string>()));
        }

        private void InitializeFakeObjects()
        {
            _generateAuthorizationResponseFake = new Mock<IGenerateAuthorizationResponse>();
            _consentRepository = new Mock<IConsentRepository>();
            _getAuthorizationCodeAndTokenViaHybridWorkflowOperation =
                new GetAuthorizationCodeAndTokenViaHybridWorkflowOperation(
                    new ProcessAuthorizationRequest(
                        new Mock<IClientStore>().Object,
                        _consentRepository.Object),
                    _generateAuthorizationResponseFake.Object);
        }
    }
}
