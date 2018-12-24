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

namespace SimpleIdentityServer.Uma.Client.Policy
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Common.DTOs;
    using Newtonsoft.Json;
    using Results;
    using Shared.Responses;

    internal class GetPolicyOperation : IGetPolicyOperation
    {
        private readonly HttpClient _httpClientFactory;

        public GetPolicyOperation(HttpClient httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<GetPolicyResult> ExecuteAsync(string id, string url, string token)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(url))
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

            url = url + "/" + id;
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };
            request.Headers.Add("Authorization", "Bearer " + token);
            var httpResult = await _httpClientFactory.SendAsync(request).ConfigureAwait(false);
            var content = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                httpResult.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                return new GetPolicyResult
                {
                    ContainsError = true,
                    Error = JsonConvert.DeserializeObject<ErrorResponse>(content),
                    HttpStatus = httpResult.StatusCode
                };
            }

            return new GetPolicyResult
            {
                Content = JsonConvert.DeserializeObject<PolicyResponse>(content)
            };
        }
    }
}
