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
using SimpleIdentityServer.AccessToken.Store.InMemory;
using SimpleIdentityServer.AccountFilter.Basic;
using SimpleIdentityServer.AccountFilter.Basic.EF;
using SimpleIdentityServer.AccountFilter.Basic.EF.InMemory;
using SimpleIdentityServer.Authenticate.Basic;
using SimpleIdentityServer.Authenticate.LoginPassword;
using SimpleIdentityServer.Authenticate.SMS;
using SimpleIdentityServer.EF;
using SimpleIdentityServer.EF.Postgre;
using SimpleIdentityServer.Host;
using SimpleIdentityServer.OAuth2Introspection;
using SimpleIdentityServer.Shell;
using SimpleIdentityServer.Startup.Extensions;
using SimpleIdentityServer.Store.InMemory;
using SimpleIdentityServer.TwoFactorAuthentication.Twilio;
using SimpleIdentityServer.UserInfoIntrospection;
using SimpleIdentityServer.UserManagement;
using System;

namespace SimpleIdentityServer.Startup
{
    public class Startup
    {
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
                Scim = new ScimOptions
                {
                    IsEnabled = true,
                    EndPoint = "http://localhost:5555/"
                }
            };
            _env = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // 2. Add the dependencies needed to enable CORS
            services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()));

            // 3. Configure Simple identity server
            ConfigureBus(services);
            ConfigureOauthRepositorySqlServer(services);
            ConfigureStorageInMemory(services);
            ConfigureLogging(services);
            services.AddInMemoryAccessTokenStore(); // Add the access token into the memory.
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
            services.AddAuthentication(Host.Constants.CookieNames.TwoFactorCookieName)
                .AddCookie(Host.Constants.CookieNames.TwoFactorCookieName);
            services.AddAuthentication(Host.Constants.CookieNames.PasswordLessCookieName)
                .AddCookie(Host.Constants.CookieNames.PasswordLessCookieName);
            services.AddAuthentication(Constants.CookieName)
                .AddCookie(Constants.CookieName, opts =>
                {
                    opts.LoginPath = "/Authenticate";
                })
                .AddOAuth2Introspection(opts =>
                {
                    opts.ClientId = "SimpleIdentityServer";
                    opts.ClientSecret = "SimpleIdentityServer";
                    opts.WellKnownConfigurationUrl = "http://localhost:60000/.well-known/openid-configuration";
                })
                .AddUserInfoIntrospection(opts =>
                {
                    opts.WellKnownConfigurationUrl = "http://localhost:60000/.well-known/openid-configuration";
                });
            services.AddAuthorization(opts =>
            {
                opts.AddOpenIdSecurityPolicy(Constants.CookieName);
            });
            // 5. Configure MVC
            var mvcBuilder = services.AddMvc();
            ConfigureAccountFilters(services, mvcBuilder);
            services.AddTwoFactorSmsAuthentication(new TwoFactorTwilioOptions
            {
                TwilioAccountSid = "",
                TwilioAuthToken = "",
                TwilioFromNumber = "",
                TwilioMessage = "The activation code is {0}"
            }); // SMS TWO FACTOR AUTHENTICATION.
            services.AddOpenIdApi(_options); // API
            services.AddBasicShell(mvcBuilder);  // SHELL
            services.AddLoginPasswordAuthentication(mvcBuilder, new BasicAuthenticateOptions
            {
                IsScimResourceAutomaticallyCreated = true,
                AuthenticationOptions = new BasicAuthenticationOptions
                {
                    AuthorizationWellKnownConfiguration = "http://localhost:60004/.well-known/uma2-configuration",
                    ClientId = "OpenId",
                    ClientSecret = "z4Bp!:B@rFw4Xs+]"
                },
                ScimBaseUrl = "http://localhost:60001",
                ClaimsIncludedInUserCreation = new[]
                {
                    "sub"
                }
            });  // LOGIN & PASSWORD
            services.AddSmsAuthentication(mvcBuilder, new SmsAuthenticationOptions
            {
                Message = "The activation code is {0}",
                TwilioSmsCredentials = new Twilio.Client.TwilioSmsCredentials
                {
                    AccountSid = "",
                    AuthToken = "",
                    FromNumber = "",
                },
                IsScimResourceAutomaticallyCreated = false,
                AuthenticationOptions = new BasicAuthenticationOptions
                {
                    AuthorizationWellKnownConfiguration = "http://localhost:60004/.well-known/uma2-configuration",
                    ClientId = "OpenId",
                    ClientSecret = "z4Bp!:B@rFw4Xs+]"
                },
                ScimBaseUrl = "http://localhost:60001",
                ClaimsIncludedInUserCreation = new[]
                {
                    "sub"
                }
            }); // SMS AUTHENTICATION.
            services.AddUserManagement(mvcBuilder);  // USER MANAGEMENT
        }

        private void ConfigureAccountFilters(IServiceCollection services, IMvcBuilder mvcBuilder)
        {
            services.AddAccountFilter(mvcBuilder);
            services.AddBasicAccountFilterInMemoryEF();
        }

        private void ConfigureBus(IServiceCollection services)
        {
            // services.AddTransient<IEventPublisher, DefaultEventPublisher>();
            // Uncomment the following line to use in-memory bus (SignalR).
            services.AddSimpleBusInMemory(new InMemoryOptions
            {
                ServerName = "openid"
            });
        }

        private void ConfigureOauthRepositorySqlServer(IServiceCollection services)
        {
            var connectionString = "User ID=rocheidserver;Password=password;Host=localhost;Port=5432;Database=idserver;Pooling=true;";
            services.AddOAuthPostgresqlEF(connectionString, null);
        }

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
            app.UseAuthentication();
            //1 . Enable CORS.
            app.UseCors("AllowAll");
            // 2. Use static files.
            app.UseShellStaticFiles();
            // 3. Redirect error to custom pages.
            app.UseStatusCodePagesWithRedirects("~/Error/{0}");
            // 4. Enable SimpleIdentityServer
            app.UseOpenIdApi(_options, loggerFactory);
            // 5. Configure ASP.NET MVC
            app.UseMvc(routes =>
            {
                routes.UseSmsAuthentication();
                // routes.UseLoginPasswordAuthentication();
                routes.MapRoute("AuthArea",
                    "{area:exists}/Authenticate/{action}/{id?}",
                    new { controller = "Authenticate", action = "Index" });
                routes.UseUserManagement();
                routes.UseShell();
            });
            UseSerilogLogging(loggerFactory);
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var simpleIdentityServerContext = serviceScope.ServiceProvider.GetService<SimpleIdentityServerContext>();
                simpleIdentityServerContext.Database.EnsureCreated();
                simpleIdentityServerContext.EnsureSeedData();
            }

            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var accountFilterContext = serviceScope.ServiceProvider.GetService<AccountFilterBasicServerContext>();
                accountFilterContext.Database.EnsureCreated();
                // accountFilterContext.EnsureSeedData();
            }
        }

        private void UseSerilogLogging(ILoggerFactory logger)
        {
            logger.AddSerilog();
        }
    }
}