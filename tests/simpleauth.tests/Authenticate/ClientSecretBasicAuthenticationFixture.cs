﻿namespace SimpleAuth.Tests.Authenticate
{
    using Shared.Models;
    using SimpleAuth.Authenticate;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public sealed class ClientSecretBasicAuthenticationFixture
    {
        [Fact]
        public void When_Trying_To_Authenticate_The_Client_And_OneParameter_Is_Null_Then_Exception_Is_Thrown()
        {
            var authenticateInstruction = new AuthenticateInstruction();

            Assert.Throws<ArgumentNullException>(() => ClientSecretBasicAuthentication.AuthenticateClient(null, null));
            Assert.Throws<ArgumentNullException>(
                () => ClientSecretBasicAuthentication.AuthenticateClient(authenticateInstruction, null));
        }

        [Fact]
        public void When_Trying_To_Authenticate_The_Client_And_ThereIsNoSharedSecret_Then_Null_Is_Returned()
        {
            var authenticateInstruction = new AuthenticateInstruction
            {
                ClientSecretFromAuthorizationHeader = "notCorrectClientSecret"
            };
            var firstClient = new Client {Secrets = null};
            var secondClient = new Client
            {
                Secrets = new List<ClientSecret> {new ClientSecret {Type = ClientSecretTypes.X509Thumbprint}}
            };

            Assert.Null(ClientSecretBasicAuthentication.AuthenticateClient(authenticateInstruction, firstClient));
            Assert.Null(ClientSecretBasicAuthentication.AuthenticateClient(authenticateInstruction, secondClient));
        }

        [Fact]
        public void When_Trying_To_Authenticate_The_Client_And_Credentials_Are_Not_Correct_Then_Null_Is_Returned()
        {
            var authenticateInstruction = new AuthenticateInstruction
            {
                ClientSecretFromAuthorizationHeader = "notCorrectClientSecret"
            };
            var client = new Client
            {
                Secrets = new List<ClientSecret>
                {
                    new ClientSecret {Type = ClientSecretTypes.SharedSecret, Value = "not_correct"}
                }
            };

            var result = ClientSecretBasicAuthentication.AuthenticateClient(authenticateInstruction, client);

            Assert.Null(result);
        }

        [Fact]
        public void When_Trying_To_Authenticate_The_Client_And_Credentials_Are_Correct_Then_Client_Is_Returned()
        {
            const string clientSecret = "clientSecret";
            var authenticateInstruction = new AuthenticateInstruction
            {
                ClientSecretFromAuthorizationHeader = clientSecret
            };
            var client = new Client
            {
                Secrets = new List<ClientSecret>
                {
                    new ClientSecret {Type = ClientSecretTypes.SharedSecret, Value = clientSecret}
                }
            };

            var result = ClientSecretBasicAuthentication.AuthenticateClient(authenticateInstruction, client);

            Assert.NotNull(result);
        }

        [Fact]
        public void When_Requesting_ClientId_And_Instruction_Is_Null_Then_Exception_Is_Thrown()
        {
            Assert.Throws<ArgumentNullException>(() => ClientSecretBasicAuthentication.GetClientId(null));
        }

        [Fact]
        public void When_Requesting_ClientId_Then_ClientId_Is_Returned()
        {
            const string clientId = "clientId";
            var instruction = new AuthenticateInstruction {ClientIdFromAuthorizationHeader = clientId};

            var result = ClientSecretBasicAuthentication.GetClientId(instruction);

            Assert.Equal(result, clientId);
        }
    }
}
