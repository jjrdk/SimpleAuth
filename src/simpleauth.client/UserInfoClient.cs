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
    using Newtonsoft.Json.Linq;
    using Results;
    using Shared.Responses;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the user info client.
    /// </summary>
    public class UserInfoClient
    {
        private readonly HttpClient _client;
        private readonly GetDiscoveryOperation _getDiscoveryOperation;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserInfoClient"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        public UserInfoClient(HttpClient client)
        {
            _client = client;
            _getDiscoveryOperation = new GetDiscoveryOperation(client);
        }

        /// <summary>
        /// Gets the specified user info based on the configuration URL and access token.
        /// </summary>
        /// <param name="configurationUrl">The configuration URL.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="inBody">if set to <c>true</c> [in body].</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// configurationUrl
        /// or
        /// accessToken
        /// </exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task<GetUserInfoResult> Get(string configurationUrl, string accessToken, bool inBody = false)
        {
            if (string.IsNullOrWhiteSpace(configurationUrl))
            {
                throw new ArgumentNullException(nameof(configurationUrl));
            }

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            if (!Uri.TryCreate(configurationUrl, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException(string.Format(ErrorDescriptions.TheUrlIsNotWellFormed, configurationUrl));
            }

            var discoveryDocument = await _getDiscoveryOperation.Execute(uri).ConfigureAwait(false);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(discoveryDocument.UserInfoEndPoint)
            };
            request.Headers.Add("Accept", "application/json");

            if (inBody)
            {
                request.Method = HttpMethod.Post;
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {
                        "access_token", accessToken
                    }
                });
            }
            else
            {
                request.Method = HttpMethod.Get;
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
            }

            var serializedContent = await _client.SendAsync(request).ConfigureAwait(false);
            var json = await serializedContent.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                serializedContent.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                return new GetUserInfoResult
                {
                    ContainsError = true,
                    Error = JsonConvert.DeserializeObject<ErrorResponseWithState>(json),
                    Status = serializedContent.StatusCode
                };
            }

            var contentType = serializedContent.Content.Headers.ContentType;
            if (contentType?.Parameters != null && contentType.MediaType == "application/jwt")
            {
                return new GetUserInfoResult
                {
                    ContainsError = false,
                    JwtToken = json
                };
            }

            if (!string.IsNullOrWhiteSpace(json))
            {
                return new GetUserInfoResult
                {
                    ContainsError = false,
                    Content = string.IsNullOrWhiteSpace(json) ? null : JObject.Parse(json)
                };
            }
            return new GetUserInfoResult
            {
                ContainsError = true,
                Error = new ErrorResponseWithState
                {
                    Error = "invalid_token",
                    ErrorDescription = "Not a valid resource owner token"
                }
            };
        }
    }
}
