﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleIdentityServer.Module;
using System;
using System.Collections.Generic;

namespace SimpleIdentityServer.OAuth2Introspection
{
    public class OAuth2IntrospectionModule : IModule
    {
        public const string OauthIntrospectClientId = "OauthIntrospectClientId";
        public const string OauthIntrospectClientSecret = "OauthIntrospectClientSecret";
        public const string OauthIntrospectAuthUrl = "OauthIntrospectAuthUrl";

        public void Configure(IApplicationBuilder applicationBuilder)
        {
        }

        public void Configure(IRouteBuilder routeBuilder)
        {
        }

        public void ConfigureServices(IServiceCollection services, IMvcBuilder mvcBuilder = null, IHostingEnvironment env = null, IDictionary<string, string> options = null, AuthenticationBuilder authBuilder = null)
        {
            if (mvcBuilder == null)
            {
                throw new ArgumentNullException(nameof(mvcBuilder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (authBuilder == null)
            {
                throw new ArgumentNullException(nameof(authBuilder));
            }

            if (!options.ContainsKey(OauthIntrospectClientId))
            {
                throw new ModuleException("configuration", $"The {OauthIntrospectClientId} configuration is missing");
            }

            if (!options.ContainsKey(OauthIntrospectClientSecret))
            {
                throw new ModuleException("configuration", $"The {OauthIntrospectClientSecret} configuration is missing");
            }

            if (!options.ContainsKey(OauthIntrospectAuthUrl))
            {
                throw new ModuleException("configuration", $"The {OauthIntrospectAuthUrl} configuration is missing");
            }

            authBuilder.AddOAuth2Introspection(opts =>
                {
                    opts.ClientId = options[OauthIntrospectClientId];
                    opts.ClientSecret = options[OauthIntrospectClientSecret];
                    opts.WellKnownConfigurationUrl = options[OauthIntrospectAuthUrl];
                });
        }

        public IEnumerable<string> GetOptionKeys()
        {
            return new[]
            {
                OauthIntrospectClientId,
                OauthIntrospectClientSecret,
                OauthIntrospectAuthUrl
            };
        }
    }
}
