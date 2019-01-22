﻿// Copyright © 2018 Habart Thierry, © 2018 Jacob Reimers
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

namespace SimpleAuth.Uma.Client.Permission
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Configuration;
    using Helpers;
    using Newtonsoft.Json;
    using Results;
    using Shared.DTOs;
    using SimpleAuth.Shared.Responses;

    internal class PermissionClient
    {
        private const string JsonMimeType = "application/json";
        private const string AuthorizationHeader = "Authorization";
        private const string Bearer = "Bearer ";
        private readonly HttpClient _client;
        private readonly IGetConfigurationOperation _getConfigurationOperation;

        public PermissionClient(HttpClient client, IGetConfigurationOperation getConfigurationOperation)
        {
            _client = client;
            _getConfigurationOperation = getConfigurationOperation;
        }

        public async Task<AddPermissionResult> AddByResolution(PostPermission request, string url, string token)
        {
            var configuration = await _getConfigurationOperation.ExecuteAsync(UriHelpers.GetUri(url)).ConfigureAwait(false);
            return await Add(request, configuration.PermissionEndpoint, token).ConfigureAwait(false);
        }

        public async Task<AddPermissionResult> AddByResolution(IEnumerable<PostPermission> request, string url, string token)
        {
            var configurationUri = UriHelpers.GetUri(url);
            var configuration = await _getConfigurationOperation.ExecuteAsync(configurationUri).ConfigureAwait(false);
            return await Add(request, configuration.PermissionEndpoint, token).ConfigureAwait(false);
        }

        public async Task<AddPermissionResult> Add(PostPermission request, string url, string token)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            var serializedPostPermission = JsonConvert.SerializeObject(request);
            var body = new StringContent(serializedPostPermission, Encoding.UTF8, JsonMimeType);
            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = body,
                RequestUri = new Uri(url)
            };
            httpRequest.Headers.Add(AuthorizationHeader, Bearer + token);
            var result = await _client.SendAsync(httpRequest).ConfigureAwait(false);
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                result.EnsureSuccessStatusCode();
            }
            catch
            {
                return new AddPermissionResult
                {
                    ContainsError = true,
                    HttpStatus = result.StatusCode,
                    Error = JsonConvert.DeserializeObject<ErrorResponse>(content)
                };
            }

            return new AddPermissionResult
            {
                Content = JsonConvert.DeserializeObject<AddPermissionResponse>(content)
            };
        }

        public async Task<AddPermissionResult> Add(IEnumerable<PostPermission> request, string url, string token)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (url.EndsWith("/"))
            {
                url = url.Remove(0, url.Length - 1);
            }

            url = url + "/bulk";

            var serializedPostPermission = JsonConvert.SerializeObject(request);
            var body = new StringContent(serializedPostPermission, Encoding.UTF8, JsonMimeType);
            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = body,
                RequestUri = new Uri(url)
            };
            httpRequest.Headers.Add(AuthorizationHeader, Bearer + token);
            var result = await _client.SendAsync(httpRequest).ConfigureAwait(false);
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                result.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                return new AddPermissionResult
                {
                    ContainsError = true,
                    HttpStatus = result.StatusCode,
                    Error = JsonConvert.DeserializeObject<ErrorResponse>(content)
                };
            }

            return new AddPermissionResult
            {
                Content = JsonConvert.DeserializeObject<AddPermissionResponse>(content)
            };
        }
    }
}
