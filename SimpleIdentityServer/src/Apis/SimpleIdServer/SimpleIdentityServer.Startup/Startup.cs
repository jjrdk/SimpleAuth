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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using SimpleBus.InMemory;
// using SimpleIdentityServer.Authenticate.Basic;
using SimpleIdentityServer.Core;
using SimpleIdentityServer.EF;
using SimpleIdentityServer.EF.SqlServer;
using SimpleIdentityServer.EventStore.Handler;
using SimpleIdentityServer.Host;
using SimpleIdentityServer.Startup.Extensions;
using System;
using System.Collections.Generic;

namespace SimpleIdentityServer.Startup
{
    public class Startup
    {
        private ModuleLoader _moduleLoader;
        private IdentityServerOptions _options;
        private IHostingEnvironment _env;
        public IConfigurationRoot Configuration { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            _options = new IdentityServerOptions
            {
                Authenticate = new AuthenticateOptions
                {
                    CookieName = Constants.CookieName,
                    ExternalCookieName = Constants.ExternalCookieName
                },
                Scim = new ScimOptions
                {
                    IsEnabled = true,
                    EndPoint = "http://localhost:5555/"
                }
            };
            _env = env;
            _moduleLoader = new ModuleLoader();
            _moduleLoader.LoadModules();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // 2. Add the dependencies needed to enable CORS
            services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()));

            // 3. Configure Simple identity server
            ConfigureEventStoreSqlServerBus(services);
            // ConfigureOauthRepositorySqlServer(services);
            ConfigureStorageInMemory(services);
            ConfigureLogging(services);
            services.AddOpenIdApi(_options);
            // 4. Enable logging
            services.AddLogging();
            services.AddAuthentication(Constants.ExternalCookieName)
                .AddCookie(Constants.ExternalCookieName)
                .AddFacebook(opts =>
                {
                    opts.ClientId = "569242033233529";
                    opts.ClientSecret = "12e0f33817634c0a650c0121d05e53eb";
                    opts.SignInScheme = Constants.ExternalCookieName;
                    opts.Scope.Add("public_profile");
                    opts.Scope.Add("email");
                });
            services.AddAuthentication(Host.Constants.TwoFactorCookieName)
                .AddCookie(Host.Constants.TwoFactorCookieName);
            services.AddAuthentication(Constants.CookieName)
                .AddCookie(Constants.CookieName, opts =>
                {
                    opts.LoginPath = "/Authenticate";
                });
            // 5. Configure MVC
            var mvcBuilder = services.AddMvc()
                /*
                .AddRazorOptions(o =>
                {
                    foreach(var module in _moduleLoader.GetModules())
                    {
                        var location = module.GetType().Assembly.Location;
                        o.AdditionalCompilationReferences.Add(MetadataReference.CreateFromFile(location));
                    }
                })
                */;
            services.AddAuthenticationWebsite(mvcBuilder, _env);
            // services.AddBasicAuthentication(mvcBuilder, _env);
            _moduleLoader.ConfigureServices(services, mvcBuilder, _env, new Dictionary<string, string>
            {
                { "OAuthConnectionString", Configuration["Db:OpenIdConnectionString"] }
            });
        }

        private void ConfigureEventStoreSqlServerBus(IServiceCollection services)
        {
            var connectionString = Configuration["Db:EvtStoreConnectionString"];
            services.AddEventStoreSqlServerEF(connectionString, null);
            services.AddSimpleBusInMemory();
            services.AddEventStoreBusHandler(new EventStoreHandlerOptions(ServerTypes.OPENID));
        }

        /*
        private void ConfigureOauthRepositorySqlServer(IServiceCollection services)
        {
            var connectionString = Configuration["Db:OpenIdConnectionString"];
            services.AddOAuthSqlServerEF(connectionString, null);
        }
        */

        private void ConfigureStorageInMemory(IServiceCollection services)
        {
            services.AddInMemoryStorage();
        }

        private void ConfigureLogging(IServiceCollection services)
        {
            Func<LogEvent, bool> serilogFilter = (e) =>
            {
                var ctx = e.Properties["SourceContext"];
                var contextValue = ctx.ToString()
                    .TrimStart('"')
                    .TrimEnd('"');
                return contextValue.StartsWith("SimpleIdentityServer") ||
                    e.Level == LogEventLevel.Error ||
                    e.Level == LogEventLevel.Fatal;
            };
            var logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.ColoredConsole();
            var log = logger.Filter.ByIncludingOnly(serilogFilter)
                .CreateLogger();
            Log.Logger = log;
            services.AddLogging();
            services.AddSingleton<Serilog.ILogger>(log);
        }

        public void Configure(IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory)
        {
            //1 . Enable CORS.
            app.UseCors("AllowAll");
            // 2. Use static files.
            app.UseStaticFiles();
            // 3. Redirect error to custom pages.
            app.UseStatusCodePagesWithRedirects("~/Error/{0}");
            // 4. Enable SimpleIdentityServer
            app.UseOpenIdApi(_options, loggerFactory);
            // 5. Configure ASP.NET MVC
            app.UseMvc(routes =>
            {
                routes.MapRoute("Error401Route",
                    Host.Constants.EndPoints.Get401,
                    new
                    {
                        controller = "Error",
                        action = "Get401"
                    });
                routes.MapRoute("Error404Route",
                    Host.Constants.EndPoints.Get404,
                    new
                    {
                        controller = "Error",
                        action = "Get404"
                    });
                routes.MapRoute("Error500Route",
                    Host.Constants.EndPoints.Get500,
                    new
                    {
                        controller = "Error",
                        action = "Get500"
                    });
                // routes.UseUserPasswordAuthentication();
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
            UseSerilogLogging(loggerFactory);

            /*
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var simpleIdentityServerContext = serviceScope.ServiceProvider.GetService<SimpleIdentityServerContext>();
                simpleIdentityServerContext.Database.EnsureCreated();
                simpleIdentityServerContext.EnsureSeedData();
            }
            */

            _moduleLoader.Configure(app);
        }

        private void UseSerilogLogging(ILoggerFactory logger)
        {
            logger.AddSerilog();
        }
    }
}
