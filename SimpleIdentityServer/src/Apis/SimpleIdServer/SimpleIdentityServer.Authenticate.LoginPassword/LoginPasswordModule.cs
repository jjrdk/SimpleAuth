﻿using SimpleIdentityServer.Authenticate.Basic;
using SimpleIdentityServer.Module;
using System.Collections.Generic;

namespace SimpleIdentityServer.Authenticate.LoginPassword
{
    public class LoginPasswordModule : IModule
    {
        private IDictionary<string, string> _properties;

        public void Init(IDictionary<string, string> properties)
        {
            _properties = properties;
            AspPipelineContext.Instance().ConfigureServiceContext.MvcAdded += HandleMvcAdded;
            AspPipelineContext.Instance().ApplicationBuilderContext.RouteConfigured += HandleRouteConfigured;
        }

        private void HandleMvcAdded(object sender, System.EventArgs e)
        {
            var configureServiceContext = AspPipelineContext.Instance().ConfigureServiceContext;
            configureServiceContext.Services.AddLoginPasswordAuthentication(configureServiceContext.MvcBuilder, GetOptions());
        }

        private void HandleRouteConfigured(object sender, System.EventArgs e)
        {
            var applicationBuilderContext = AspPipelineContext.Instance().ApplicationBuilderContext;
            applicationBuilderContext.RouteBuilder.UseLoginPasswordAuthentication();
        }

        private BasicAuthenticateOptions GetOptions()
        {
            var result = new BasicAuthenticateOptions();
            if (_properties != null)
            {
                if (_properties.TryGetValue("ScimBaseUrl", out var scimBaseUrl))
                {
                    result.ScimBaseUrl = scimBaseUrl;
                }

                if (_properties.TryGetValue("IsScimResourceAutomaticallyCreated", out bool isScimResourceAutomaticallyCreated))
                {
                    result.IsScimResourceAutomaticallyCreated = isScimResourceAutomaticallyCreated;
                }

                if (_properties.TryGetValue("ClientId", out var clientId))
                {
                    result.AuthenticationOptions.ClientId = clientId;
                }

                if (_properties.TryGetValue("ClientSecret", out var clientSecret))
                {
                    result.AuthenticationOptions.ClientSecret = clientSecret;
                }

                if (_properties.TryGetValue("AuthorizationWellKnownConfiguration", out var authorizationWellKnownConfiguration))
                {
                    result.AuthenticationOptions.AuthorizationWellKnownConfiguration = authorizationWellKnownConfiguration;
                }
                result.ClaimsIncludedInUserCreation.Clear();
                result.ClaimsIncludedInUserCreation.AddRange(_properties.TryGetArr("ClaimsIncludedInUserCreation"));
            }

            return result;
        }
    }
}
