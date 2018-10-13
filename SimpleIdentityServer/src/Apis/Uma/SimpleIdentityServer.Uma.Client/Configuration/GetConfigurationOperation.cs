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

namespace SimpleIdentityServer.Uma.Client.Configuration
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Common.DTOs;
    using Newtonsoft.Json;

    public interface IGetConfigurationOperation
    {
        Task<ConfigurationResponse> ExecuteAsync(Uri configurationUri);
    }

    public class GetConfigurationOperation : IGetConfigurationOperation
    {
        private readonly HttpClient _httpClientFactory;

        public GetConfigurationOperation(HttpClient httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ConfigurationResponse> ExecuteAsync(Uri configurationUri)
        {
            if (configurationUri == null)
            {
                throw new ArgumentNullException(nameof(configurationUri));
            }

            var result = await _httpClientFactory.GetStringAsync(configurationUri).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<ConfigurationResponse>(result);
        }
    }
}
