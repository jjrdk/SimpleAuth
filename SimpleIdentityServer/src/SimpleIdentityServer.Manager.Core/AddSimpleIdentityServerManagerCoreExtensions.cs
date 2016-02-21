﻿#region copyright
// Copyright 2015 Habart Thierry
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
#endregion

using Microsoft.Extensions.DependencyInjection;
using SimpleIdentityServer.Manager.Core.Api.Jws;
using SimpleIdentityServer.Manager.Core.Api.Jws.Actions;
using SimpleIdentityServer.Manager.Core.Factories;
using SimpleIdentityServer.Manager.Core.Helpers;

namespace SimpleIdentityServer.Manager.Core
{
    public static class AddSimpleIdentityServerManagerCoreExtensions
    {
        public static IServiceCollection AddSimpleIdentityServerManagerCore(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IJwsActions, JwsActions>();
            serviceCollection.AddTransient<IGetJwsInformationAction, GetJwsInformationAction>();
            serviceCollection.AddTransient<ICreateJwsAction, CreateJwsAction>();
            serviceCollection.AddTransient<IJsonWebKeyHelper, JsonWebKeyHelper>();
            serviceCollection.AddTransient<IHttpClientFactory, HttpClientFactory>();
            serviceCollection.AddTransient<IJsonWebKeyEnricher, JsonWebKeyEnricher>();
            return serviceCollection;
        }
    }
}