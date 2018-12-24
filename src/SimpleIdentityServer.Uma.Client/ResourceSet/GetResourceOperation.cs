﻿// Copyright 2016 Habart Thierry
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

namespace SimpleIdentityServer.Uma.Client.ResourceSet
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Common.DTOs;
    using Newtonsoft.Json;
    using Results;
    using SimpleAuth.Shared.Responses;

    internal class GetResourceOperation : IGetResourceOperation
    {
        private readonly HttpClient _httpClientFactory;

        public GetResourceOperation(HttpClient httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<GetResourceSetResult> ExecuteAsync(string resourceSetId, string resourceSetUrl, string authorizationHeaderValue)
        {
            if (string.IsNullOrWhiteSpace(resourceSetId))
            {
                throw new ArgumentNullException(nameof(resourceSetId));
            }

            if (string.IsNullOrWhiteSpace(resourceSetUrl))
            {
                throw new ArgumentNullException(nameof(resourceSetUrl));
            }

            if (string.IsNullOrWhiteSpace(authorizationHeaderValue))
            {
                throw new ArgumentNullException(nameof(authorizationHeaderValue));
            }

            if (resourceSetUrl.EndsWith("/"))
            {
                resourceSetUrl = resourceSetUrl.Remove(0, resourceSetUrl.Length - 1);
            }

            resourceSetUrl = resourceSetUrl + "/" + resourceSetId;
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(resourceSetUrl)
            };
            request.Headers.Add("Authorization", "Bearer " + authorizationHeaderValue);
            var httpResult = await _httpClientFactory.SendAsync(request).ConfigureAwait(false);
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                httpResult.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                return new GetResourceSetResult
                {
                    ContainsError = true,
                    Error = JsonConvert.DeserializeObject<ErrorResponse>(json),
                    HttpStatus = httpResult.StatusCode
                };
            }

            return new GetResourceSetResult
            {
                Content = JsonConvert.DeserializeObject<ResourceSetResponse>(json)
            };
        }
    }
}
