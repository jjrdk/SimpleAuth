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

namespace SimpleAuth.Authenticate
{
    using Errors;
    using Shared.Models;
    using Shared.Repositories;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class AuthenticateClient
    {
        private readonly ClientSecretBasicAuthentication _clientSecretBasicAuthentication;
        private readonly ClientSecretPostAuthentication _clientSecretPostAuthentication;
        private readonly ClientAssertionAuthentication _clientAssertionAuthentication;
        private readonly ClientTlsAuthentication _clientTlsAuthentication;
        private readonly IClientStore _clientRepository;

        public AuthenticateClient(IClientStore clientRepository)
        {
            _clientSecretBasicAuthentication = new ClientSecretBasicAuthentication();
            _clientSecretPostAuthentication = new ClientSecretPostAuthentication();
            _clientAssertionAuthentication = new ClientAssertionAuthentication(clientRepository);
            _clientTlsAuthentication = new ClientTlsAuthentication();
            _clientRepository = clientRepository;
        }

        public async Task<AuthenticationResult> Authenticate(AuthenticateInstruction instruction, string issuerName)
        {
            if (instruction == null)
            {
                throw new ArgumentNullException(nameof(instruction));
            }

            Client client = null;
            // First we try to fetch the client_id
            // The different client authentication mechanisms are described here : http://openid.net/specs/openid-connect-core-1_0.html#ClientAuthentication
            var clientId = TryGettingClientId(instruction);
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                client = await _clientRepository.GetById(clientId).ConfigureAwait(false);
            }

            if (client == null)
            {
                return new AuthenticationResult(null, ErrorDescriptions.TheClientDoesntExist);
            }

            var errorMessage = string.Empty;
            switch (client.TokenEndPointAuthMethod)
            {
                case TokenEndPointAuthenticationMethods.client_secret_basic:
                    client = _clientSecretBasicAuthentication.AuthenticateClient(instruction, client);
                    if (client == null)
                    {
                        errorMessage = ErrorDescriptions.TheClientCannotBeAuthenticatedWithSecretBasic;
                    }
                    break;
                case TokenEndPointAuthenticationMethods.client_secret_post:
                    client = _clientSecretPostAuthentication.AuthenticateClient(instruction, client);
                    if (client == null)
                    {
                        errorMessage = ErrorDescriptions.TheClientCannotBeAuthenticatedWithSecretPost;
                    }
                    break;
                case TokenEndPointAuthenticationMethods.client_secret_jwt:
                    if (client.Secrets == null || client.Secrets.All(s => s.Type != ClientSecretTypes.SharedSecret))
                    {
                        errorMessage = string.Format(ErrorDescriptions.TheClientDoesntContainASharedSecret, client.ClientId);
                        break;
                    }
                    return await _clientAssertionAuthentication.AuthenticateClientWithClientSecretJwtAsync(instruction).ConfigureAwait(false);
                case TokenEndPointAuthenticationMethods.private_key_jwt:
                    return await _clientAssertionAuthentication.AuthenticateClientWithPrivateKeyJwtAsync(instruction, issuerName).ConfigureAwait(false);
                case TokenEndPointAuthenticationMethods.tls_client_auth:
                    client = _clientTlsAuthentication.AuthenticateClient(instruction, client);
                    if (client == null)
                    {
                        errorMessage = ErrorDescriptions.TheClientCannotBeAuthenticatedWithTls;
                    }
                    break;
            }

            return new AuthenticationResult(client, errorMessage);
        }

        /// <summary>
        /// Try to get the client id from the HTTP body or HTTP header.
        /// </summary>
        /// <param name="instruction">Authentication instruction</param>
        /// <returns>Client id</returns>
        private string TryGettingClientId(AuthenticateInstruction instruction)
        {
            var clientId = _clientAssertionAuthentication.GetClientId(instruction);
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                return clientId;
            }

            clientId = _clientSecretBasicAuthentication.GetClientId(instruction);
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                return clientId;
            }

            return _clientSecretPostAuthentication.GetClientId(instruction);
        }
    }
}
