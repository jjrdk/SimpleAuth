#region copyright
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
using SimpleBus.Core;
using SimpleIdentityServer.EF;
using SimpleIdentityServer.EF.SqlServer;
using SimpleIdentityServer.OAuth2Introspection;
using SimpleIdentityServer.Store.InMemory;
using SimpleIdentityServer.Uma.EF;
using SimpleIdentityServer.Uma.EF.InMemory;
using SimpleIdentityServer.Uma.Host.Configurations;
using SimpleIdentityServer.Uma.Host.Extensions;
using SimpleIdentityServer.Uma.Host.Middlewares;
using SimpleIdentityServer.Uma.Logging;
using SimpleIdentityServer.Uma.Startup.Extensions;
using SimpleIdentityServer.Uma.Startup.Services;
using SimpleIdentityServer.Uma.Store.InMemory;
using SimpleIdentityServer.UserInfoIntrospection;
using System;
using WebApiContrib.Core.Concurrency;
using WebApiContrib.Core.Storage.InMemory;

namespace SimpleIdentityServer.Uma.Startup
{
    public class Startup
    {
        private UmaHostConfiguration _umaHostConfiguration;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
            _umaHostConfiguration = new UmaHostConfiguration();
        }

        public IConfigurationRoot Configuration { get; set; }
        
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureBus(services);
            ConfigureOauthRepositorySqlServer(services);
            ConfigureUmaInMemoryEF(services);
            ConfigureUmaInMemoryStore(services);
            ConfigureStorageInMemory(services);
            ConfigureLogging(services);
            ConfigureCaching(services);
            services.AddAuthentication(OAuth2IntrospectionOptions.AuthenticationScheme)
                .AddOAuth2Introspection(opts =>
                {
                    opts.ClientId = "uma";
                    opts.ClientSecret = "uma";
                    opts.WellKnownConfigurationUrl = "http://localhost:60004/.well-known/uma2-configuration";
                })
		        .AddUserInfoIntrospection(opts =>
                {
                    opts.WellKnownConfigurationUrl = "http://localhost:60000/.well-known/openid-configuration";
                });
            services.AddAuthorization(opts =>
            {
                opts.AddUmaSecurityPolicy();
            });
            services.AddLogging();
            services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()));
            services.AddUmaHost(_umaHostConfiguration);
	        services.AddMvc();
        }

        private void ConfigureBus(IServiceCollection services)
        {
            services.AddTransient<IEventPublisher, DefaultEventPublisher>();
            // Uncomment the following line to use in-memory bus.
            /*
            services.AddSimpleBusInMemory(new SimpleBus.Core.SimpleBusOptions
            {
                ServerName = "auth"
            });
            */
        }

        private void ConfigureOauthRepositorySqlServer(IServiceCollection services)
        {
            var connectionString = "Data Source=.;Initial Catalog=SimpleIdServerOauthUma;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            services.AddOAuthSqlServerEF(connectionString, null);
        }

        private void ConfigureUmaInMemoryEF(IServiceCollection services)
        {
            // var connectionString = "Data Source=.;Initial Catalog=SimpleIdServerUma;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            services.AddUmaInMemoryEF();
        }

        private void ConfigureUmaInMemoryStore(IServiceCollection services)
        {
            services.AddUmaInMemoryStore();
        }

        private void ConfigureStorageInMemory(IServiceCollection services)
        {
            services.AddInMemoryStorage();
        }

        private void ConfigureCaching(IServiceCollection services)
        {
            services.AddConcurrency(opt => opt.UseInMemory());
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

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            UseSerilogLogging(loggerFactory);
            app.UseAuthentication();
            app.UseCors("AllowAll");
            app.UseUmaExceptionHandler(new ExceptionHandlerMiddlewareOptions
            {
                UmaEventSource = app.ApplicationServices.GetService<IUmaServerEventSource>()
            });
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}");
            });
            // Insert the data.
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var simpleIdServerUmaContext = serviceScope.ServiceProvider.GetService<SimpleIdServerUmaContext>();
                simpleIdServerUmaContext.Database.EnsureCreated();
                simpleIdServerUmaContext.EnsureSeedData();
            }

            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var simpleIdentityServerContext = serviceScope.ServiceProvider.GetService<SimpleIdentityServerContext>();
                simpleIdentityServerContext.Database.EnsureCreated();
                simpleIdentityServerContext.EnsureSeedData();
            }
        }

        private void UseSerilogLogging(ILoggerFactory logger)
        {
            logger.AddSerilog();
        }
    }
}
