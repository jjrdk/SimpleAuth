﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleIdentityServer.Module;
using System.Collections.Generic;

namespace SimpleIdentityServer.Host
{
    public class SimpleIdentityServerHostModule : IModule
    {
        // private const string OpenIdCookieName = "OpenIdCookieName";
        // private const string OpenIdExternalCookieName = "OpenIdExternalCookieName";
        // private const string ScimEndpoint = "ScimEndpoint";
        // private const string ScimEndpointEnabled = "ScimEndpointEnabled";

        public void Init()
        {
            AspPipelineContext.Instance().ConfigureServiceContext.Initialized += HandleServiceContextInitialized;
            AspPipelineContext.Instance().ConfigureServiceContext.AuthorizationAdded += HandleAuthorizationAdded;
            AspPipelineContext.Instance().ApplicationBuilderContext.Initialized += HandleApplicationBuilderInitialized;
        }

        private void HandleAuthorizationAdded(object sender, System.EventArgs e)
        {
            AspPipelineContext.Instance().ConfigureServiceContext.AuthorizationOptions.AddOpenIdSecurityPolicy(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        private void HandleServiceContextInitialized(object sender, System.EventArgs e)
        {
            AspPipelineContext.Instance().ConfigureServiceContext.Services.AddOpenIdApi(o => { });
        }

        private void HandleApplicationBuilderInitialized(object sender, System.EventArgs e)
        {
            AspPipelineContext.Instance().ApplicationBuilderContext.App.UseOpenIdApi(new IdentityServerOptions());
        }

        public void Configure(IApplicationBuilder applicationBuilder)
        {
            /*
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            applicationBuilder.UseSimpleIdentityServerExceptionHandler(new ExceptionHandlerMiddlewareOptions
            {
                SimpleIdentityServerEventSource = applicationBuilder.ApplicationServices.GetService<IOpenIdEventSource>()
            });
            var httpContextAccessor = applicationBuilder.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            Extensions.UriHelperExtensions.Configure(httpContextAccessor);
            */
        }

        public void Configure(IRouteBuilder routeBuilder)
        {

        }

        public void ConfigureServices(IServiceCollection services, IMvcBuilder mvcBuilder = null, IHostingEnvironment env = null, IDictionary<string, string> options = null, IEnumerable<ModuleUIDescriptor> moduleUiDescriptors = null)
        {
            /*
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
                throw new ArgumentNullException(nameof(mvcBuilder));
            }

            var opts = GetOptions(options);
            var assembly = typeof(AuthorizationController).Assembly;
            var embeddedFileProvider = new EmbeddedFileProvider(assembly);
            services.Configure<RazorViewEngineOptions>(o =>
            {
                o.FileProviders.Add(embeddedFileProvider);
            });

            mvcBuilder.AddApplicationPart(assembly);
            services.AddOpenIdApi(opts);
            */
        }

        public void ConfigureAuthorization(AuthorizationOptions authorizationOptions, IDictionary<string, string> options = null)
        {
            // var opts = GetOptions(options);
            // authorizationOptions.AddOpenIdSecurityPolicy(opts.Authenticate.CookieName);
        }

        public void ConfigureAuthentication(AuthenticationBuilder authBuilder, IDictionary<string, string> options = null)
        {
        }

        public IEnumerable<string> GetOptionKeys()
        {
            /*
            return new[]
            {
                OpenIdCookieName,
                OpenIdExternalCookieName,
                ScimEndpoint,
                ScimEndpointEnabled
            };
            */
            return null;
        }

        private static IdentityServerOptions GetOptions(IDictionary<string, string> options)
        {
            /*
            var opts = new IdentityServerOptions();
            if (opts == null)
            {
                return opts;
            }

            if (options.ContainsKey(OpenIdCookieName))
            {
                opts.Authenticate.CookieName = options[OpenIdCookieName];
            }

            if (options.ContainsKey(OpenIdExternalCookieName))
            {
                opts.Authenticate.ExternalCookieName = options[OpenIdExternalCookieName];
            }

            if (options.ContainsKey(ScimEndpointEnabled))
            {
                bool b;
                if (bool.TryParse(options[ScimEndpointEnabled], out b))
                {
                    opts.Scim.IsEnabled = b;
                }
            }

            if (options.ContainsKey(ScimEndpoint))
            {
                opts.Scim.EndPoint = options[ScimEndpoint];
            }

            return opts;
            */
            return null;
        }

        public ModuleUIDescriptor GetModuleUI()
        {
            return null;
        }
    }
}
