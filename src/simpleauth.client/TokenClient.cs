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

namespace SimpleAuth.Client
{
    using Newtonsoft.Json;
    using Results;
    using Shared.Responses;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    public class TokenClient
    {
        private readonly DiscoveryInformation _discoveryInformation;
        private readonly string _authorizationValue;
        private readonly X509Certificate2 _certificate;
        private readonly HttpClient _client;
        private readonly TokenCredentials _form;

        private TokenClient(TokenCredentials credentials, HttpClient client, DiscoveryInformation discoveryInformation)
        {
            _form = credentials;
            _client = client;
            _discoveryInformation = discoveryInformation;
            _authorizationValue = credentials.AuthorizationValue;
            _certificate = credentials.Certificate;
        }

        public static async Task<TokenClient> Create(
            TokenCredentials credentials,
            HttpClient client,
            Uri discoveryDocumentationUrl)
        {
            if (!discoveryDocumentationUrl.IsAbsoluteUri)
            {
                throw new ArgumentException(
                    string.Format(ErrorDescriptions.TheUrlIsNotWellFormed, discoveryDocumentationUrl));
            }

            var operation = new GetDiscoveryOperation(client);
            var discoveryInformation = await operation.Execute(discoveryDocumentationUrl).ConfigureAwait(false);
            return new TokenClient(credentials, client, discoveryInformation);
        }

        public async Task<BaseSidContentResult<GrantedTokenResponse>> GetToken(TokenRequest tokenRequest)
        {
            var body = new FormUrlEncodedContent(_form.Concat(tokenRequest));
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = body,
                RequestUri = new Uri(_discoveryInformation.TokenEndPoint)
            };
            if (_certificate != null)
            {
                var bytes = _certificate.RawData;
                var base64Encoded = Convert.ToBase64String(bytes);
                request.Headers.Add("X-ARR-ClientCert", base64Encoded);
            }

            if (!string.IsNullOrWhiteSpace(_authorizationValue))
            {
                request.Headers.Add("Authorization", "Basic " + _authorizationValue);
            }

            var result = await _client.SendAsync(request).ConfigureAwait(false);
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                result.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                return new BaseSidContentResult<GrantedTokenResponse>
                {
                    ContainsError = true,
                    Error = JsonConvert.DeserializeObject<ErrorResponseWithState>(content),
                    Status = result.StatusCode
                };
            }

            return new BaseSidContentResult<GrantedTokenResponse> { Content = JsonConvert.DeserializeObject<GrantedTokenResponse>(content) };
        }

        public async Task<RevokeTokenResult> RevokeToken(RevokeTokenRequest revokeTokenRequest)
        {
            var body = new FormUrlEncodedContent(_form.Concat(revokeTokenRequest));
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = body,
                RequestUri = new Uri(_discoveryInformation.RevocationEndPoint)
            };
            if (_certificate != null)
            {
                var bytes = _certificate.RawData;
                var base64Encoded = Convert.ToBase64String(bytes);
                request.Headers.Add("X-ARR-ClientCert", base64Encoded);
            }

            if (!string.IsNullOrWhiteSpace(_authorizationValue))
            {
                request.Headers.Add("Authorization", "Basic " + _authorizationValue);
            }

            var result = await _client.SendAsync(request).ConfigureAwait(false);
            var json = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                result.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                return new RevokeTokenResult
                {
                    ContainsError = true,
                    Error = JsonConvert.DeserializeObject<ErrorResponseWithState>(json),
                    Status = result.StatusCode
                };
            }

            return new RevokeTokenResult { Status = result.StatusCode };
        }
    }
}
