// Copyright � 2015 Habart Thierry, � 2018 Jacob Reimers
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

namespace SimpleIdentityServer.Host.Extensions
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using MiddleWare;
    using System;
    using Core.Logging;

    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseManagerApi(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseSimpleIdentityServerManagerExceptionHandler(new ExceptionHandlerMiddlewareOptions
            {
                SimpleIdentityServerEventSource = app.ApplicationServices.GetService<IOpenIdEventSource>()
                // ManagerEventSource = (IManagerEventSource)app.ApplicationServices.GetService(typeof(IManagerEventSource))
            });
            return app;
        }

        public static void UseOpenIdApi(this IApplicationBuilder app, Action<IdentityServerOptions> optionsCallback, ILoggerFactory loggerFactory)
        {
            if (optionsCallback == null)
            {
                throw new ArgumentNullException(nameof(optionsCallback));
            }

            var hostingOptions = new IdentityServerOptions();
            optionsCallback(hostingOptions);
            app.UseOpenIdApi(hostingOptions,
                loggerFactory);
        }

        public static void UseOpenIdApi(this IApplicationBuilder app, IdentityServerOptions options, ILoggerFactory loggerFactory)
        {
            UseOpenIdApi(app, options);
        }

        public static void UseOpenIdApi(this IApplicationBuilder app, IdentityServerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            app.UseSimpleIdentityServerExceptionHandler(new ExceptionHandlerMiddlewareOptions
            {
                SimpleIdentityServerEventSource = app.ApplicationServices.GetService<IOpenIdEventSource>()
            });
            var httpContextAccessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            UriHelperExtensions.Configure(httpContextAccessor);
        }
    }
}