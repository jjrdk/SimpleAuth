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
using SimpleIdentityServer.Uma.Core.Api.ConfigurationController;
using SimpleIdentityServer.Uma.Core.Api.ConfigurationController.Actions;
using SimpleIdentityServer.Uma.Core.Api.PermissionController;
using SimpleIdentityServer.Uma.Core.Api.PermissionController.Actions;
using SimpleIdentityServer.Uma.Core.Api.PolicyController;
using SimpleIdentityServer.Uma.Core.Api.PolicyController.Actions;
using SimpleIdentityServer.Uma.Core.Api.ResourceSetController;
using SimpleIdentityServer.Uma.Core.Api.ResourceSetController.Actions;
using SimpleIdentityServer.Uma.Core.Api.Token;
using SimpleIdentityServer.Uma.Core.Api.Token.Actions;
using SimpleIdentityServer.Uma.Core.Helpers;
using SimpleIdentityServer.Uma.Core.JwtToken;
using SimpleIdentityServer.Uma.Core.Policies;
using SimpleIdentityServer.Uma.Core.Services;
using SimpleIdentityServer.Uma.Core.Validators;
using System;

namespace SimpleIdentityServer.Uma.Core
{
    public static class SimpleIdServerUmaCoreExtensions
    {
        public static IServiceCollection AddSimpleIdServerUmaCore(
            this IServiceCollection serviceCollection,
            UmaServerOptions options = null)
        {
            if (options == null)
            {
                options = new UmaServerOptions();
            }

            RegisterDependencies(serviceCollection, options);
            return serviceCollection;
        }

        public static IServiceCollection AddSimpleIdServerUmaCore(
            this IServiceCollection serviceCollection,
            Action<UmaServerOptions> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var umaServerOptions = new UmaServerOptions();
            callback(umaServerOptions);
            RegisterDependencies(serviceCollection, umaServerOptions);
            return serviceCollection;
        }

        private static void RegisterDependencies(
            IServiceCollection serviceCollection,
            UmaServerOptions umaServerOptions)
        {
            serviceCollection.AddTransient<IResourceSetActions, ResourceSetActions>();
            serviceCollection.AddTransient<IAddResourceSetAction, AddResourceSetAction>();
            serviceCollection.AddTransient<IGetResourceSetAction, GetResourceSetAction>();
            serviceCollection.AddTransient<IUpdateResourceSetAction, UpdateResourceSetAction>();
            serviceCollection.AddTransient<IDeleteResourceSetAction, DeleteResourceSetAction>();
            serviceCollection.AddTransient<IGetAllResourceSetAction, GetAllResourceSetAction>();
            serviceCollection.AddTransient<IResourceSetParameterValidator, ResourceSetParameterValidator>();
            serviceCollection.AddTransient<IPermissionControllerActions, PermissionControllerActions>();
            serviceCollection.AddTransient<IAddPermissionAction, AddPermissionAction>();
            serviceCollection.AddTransient<IRepositoryExceptionHelper, RepositoryExceptionHelper>();
            serviceCollection.AddTransient<IAuthorizationPolicyValidator, AuthorizationPolicyValidator>();
            serviceCollection.AddTransient<IBasicAuthorizationPolicy, BasicAuthorizationPolicy>();
            serviceCollection.AddTransient<ICustomAuthorizationPolicy, CustomAuthorizationPolicy>();
            serviceCollection.AddTransient<IAddAuthorizationPolicyAction, AddAuthorizationPolicyAction>();
            serviceCollection.AddTransient<IPolicyActions, PolicyActions>();
            serviceCollection.AddTransient<IGetAuthorizationPolicyAction, GetAuthorizationPolicyAction>();
            serviceCollection.AddTransient<IDeleteAuthorizationPolicyAction, DeleteAuthorizationPolicyAction>();
            serviceCollection.AddTransient<IGetAuthorizationPoliciesAction, GetAuthorizationPoliciesAction>();
            serviceCollection.AddTransient<IUpdatePolicyAction, UpdatePolicyAction>();
            serviceCollection.AddTransient<IConfigurationActions, ConfigurationActions>();
            serviceCollection.AddTransient<IGetConfigurationAction, GetConfigurationAction>();
            serviceCollection.AddTransient<IJwtTokenParser, JwtTokenParser>();
            serviceCollection.AddTransient<IAddResourceSetToPolicyAction, AddResourceSetToPolicyAction>();
            serviceCollection.AddTransient<IDeleteResourcePolicyAction, DeleteResourcePolicyAction>();
            serviceCollection.AddTransient<IGetPoliciesAction, GetPoliciesAction>();
            serviceCollection.AddTransient<ISearchAuthPoliciesAction, SearchAuthPoliciesAction>();
            serviceCollection.AddTransient<ISearchResourceSetOperation, SearchResourceSetOperation>();
            if (umaServerOptions.ConfigurationService == null)
            {
                serviceCollection.AddTransient<IConfigurationService, DefaultConfigurationService>();
            }
            else
            {
                serviceCollection.AddSingleton<IConfigurationService>(umaServerOptions.ConfigurationService);
            }

            serviceCollection.AddTransient<IUmaTokenActions, UmaTokenActions>();
            serviceCollection.AddTransient<IGetTokenByTicketIdAction, GetTokenByTicketIdAction>();
        }
    }
}
