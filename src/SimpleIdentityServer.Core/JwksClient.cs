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

namespace SimpleIdentityServer.Core
{
    using Api.Discovery;
    using Common.DTOs.Requests;
    using Newtonsoft.Json;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class JwksClient : IJwksClient
    {
        private readonly HttpClient _client;
        private readonly IDiscoveryActions _getDiscoveryOperation;

        public JwksClient(HttpClient client, IDiscoveryActions getDiscoveryOperation)
        {
            _client = client;
            _getDiscoveryOperation = getDiscoveryOperation;
        }

        public async Task<JsonWebKeySet> ResolveAsync(Uri configurationUrl)
        {
            var builder = new UriBuilder(configurationUrl.Scheme, configurationUrl.Host, configurationUrl.Port);
            var uri = builder.Uri;
            var discoveryDocument = await _getDiscoveryOperation.CreateDiscoveryInformation(uri.ToString()).ConfigureAwait(false);
            
            var serializedContent = await _client.GetStringAsync(discoveryDocument.JwksUri).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<JsonWebKeySet>(serializedContent);
        }
    }
}
