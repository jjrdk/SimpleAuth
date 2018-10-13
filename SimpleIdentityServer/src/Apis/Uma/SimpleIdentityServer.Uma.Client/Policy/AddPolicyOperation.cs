﻿// Copyright 2015 Habart Thierry
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

namespace SimpleIdentityServer.Uma.Client.Policy
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Common.DTOs;
    using Newtonsoft.Json;
    using Results;
    using SimpleIdentityServer.Common.Dtos.Responses;

    public interface IAddPolicyOperation
    {
        Task<AddPolicyResult> ExecuteAsync(PostPolicy request, string url, string authorizationHeaderValue);
    }

    internal class AddPolicyOperation : IAddPolicyOperation
    {
        private readonly HttpClient _httpClientFactory;

        public AddPolicyOperation(HttpClient httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<AddPolicyResult> ExecuteAsync(PostPolicy request, string url, string authorizationHeaderValue)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (string.IsNullOrWhiteSpace(authorizationHeaderValue))
            {
                throw new ArgumentNullException(nameof(authorizationHeaderValue));
            }

            var serializedPostResourceSet = JsonConvert.SerializeObject(request);
            var body = new StringContent(serializedPostResourceSet, Encoding.UTF8, "application/json");
            var httpRequest = new HttpRequestMessage
            {
                Content = body,
                Method = HttpMethod.Post,
                RequestUri = new Uri(url)
            };
            httpRequest.Headers.Add("Authorization", "Bearer " + authorizationHeaderValue);
            var httpResult = await _httpClientFactory.SendAsync(httpRequest).ConfigureAwait(false);
            var content = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                httpResult.EnsureSuccessStatusCode();
            }
            catch(Exception)
            {
                return new AddPolicyResult
                {
                    ContainsError = true,
                    Error = JsonConvert.DeserializeObject<ErrorResponse>(content),
                    HttpStatus = httpResult.StatusCode
                };
            }

            return new AddPolicyResult
            {
                Content = JsonConvert.DeserializeObject<AddPolicyResponse>(content)
            };
        }
    }
}
