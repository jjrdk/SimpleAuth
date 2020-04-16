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

namespace SimpleAuth.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using SimpleAuth.Shared.Responses;

    internal class GetDiscoveryOperation
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private readonly Dictionary<string, DiscoveryInformation> _cache =
            new Dictionary<string, DiscoveryInformation>();

        private readonly Uri _discoveryDocumentationUri;
        private readonly HttpClient _httpClient;

        public GetDiscoveryOperation(Uri authority, HttpClient httpClient)
        {
            if (authority == null)
            {
                throw new ArgumentNullException(nameof(authority));
            }

            var uri = new UriBuilder(
                authority.Scheme,
                authority.Host,
                authority.Port,
                "/.well-known/openid-configuration");
            _discoveryDocumentationUri = uri.Uri;
            _httpClient = httpClient;
        }

        public async Task<DiscoveryInformation> Execute(CancellationToken cancellationToken = default)
        {
            var key = _discoveryDocumentationUri.ToString();
            try
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (_cache.TryGetValue(key, out var doc))
                {
                    return doc;
                }

                var request = new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = _discoveryDocumentationUri };
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response =
                    await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var serializedContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                doc = JsonConvert.DeserializeObject<DiscoveryInformation>(serializedContent);
                _cache.Add(key, doc);
                return doc;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
