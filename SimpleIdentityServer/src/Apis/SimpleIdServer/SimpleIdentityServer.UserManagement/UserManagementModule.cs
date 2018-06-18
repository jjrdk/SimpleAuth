﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleIdentityServer.Module;
using System;
using System.Collections.Generic;

namespace SimpleIdentityServer.UserManagement
{
    public class UserManagementModule : IModule
    {
        private const string CreateScimResourceWhenAccountIsAdded = "CreateScimResourceWhenAccountIsAdded";
        private const string ClientId = "ClientId";
        private const string ClientSecret = "ClientSecret";
        private const string AuthorizationWellKnownConfiguration = "AuthorizationWellKnownConfiguration";
        private const string ScimBaseUrl = "ScimBaseUrl";

        public static ModuleUIDescriptor ModuleUi = new ModuleUIDescriptor
        {
            Title = "UserManagement",
            RelativeUrl = "~/User",
            IsAuthenticated = true,
            Picture = "~/img/Unknown.png"
        };

        public void Configure(IApplicationBuilder applicationBuilder)
        {
        }

        public void Configure(IRouteBuilder routeBuilder)
        {
            if (routeBuilder == null)
            {
                throw new ArgumentNullException(nameof(routeBuilder));
            }

            routeBuilder.UseUserManagement();
        }

        public void ConfigureAuthentication(AuthenticationBuilder authBuilder, IDictionary<string, string> options = null)
        {
        }

        public void ConfigureAuthorization(AuthorizationOptions authorizationOptions, IDictionary<string, string> options = null)
        {
        }

        public void ConfigureServices(IServiceCollection services, IMvcBuilder mvcBuilder = null, IHostingEnvironment env = null, IDictionary<string, string> options = null, IEnumerable<ModuleUIDescriptor> moduleUIDescriptors = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (mvcBuilder == null)
            {
                throw new ArgumentNullException(nameof(mvcBuilder));
            }

            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            var opts = GetOptions(options);
            services.AddUserManagement(mvcBuilder, env, opts);
        }

        public IEnumerable<string> GetOptionKeys()
        {
            return new[]
            {
                CreateScimResourceWhenAccountIsAdded,
                ClientId,
                ClientSecret,
                AuthorizationWellKnownConfiguration,
                ScimBaseUrl,
            };
        }

        public ModuleUIDescriptor GetModuleUI()
        {
            return ModuleUi;
        }

        private static UserManagementOptions GetOptions(IDictionary<string, string> options)
        {
            var result = new UserManagementOptions();
            if (options == null)
            {
                return result;
            }

            bool createScimResourceWhenAccountIsAdded = false;
            if (options.ContainsKey(CreateScimResourceWhenAccountIsAdded))
            {
                bool.TryParse(options[CreateScimResourceWhenAccountIsAdded], out createScimResourceWhenAccountIsAdded);
            }

            result.AuthenticationOptions = new UserManagementAuthenticationOptions
            {
                AuthorizationWellKnownConfiguration = TryGetStr(options, AuthorizationWellKnownConfiguration),
                ClientId = TryGetStr(options, ClientId),
                ClientSecret = TryGetStr(options, ClientSecret)
            };
            result.ScimBaseUrl = TryGetStr(options, ScimBaseUrl);
            result.CreateScimResourceWhenAccountIsAdded = createScimResourceWhenAccountIsAdded;
            return result;
        }

        private static string TryGetStr(IDictionary<string, string> opts, string name)
        {
            if (opts.ContainsKey(name))
            {
                return opts[name];
            }

            return null;
        }
    }
}
