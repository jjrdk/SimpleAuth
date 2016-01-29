﻿using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNet.Authentication.Cookies;
using SimpleIdentityServer.Core;

namespace SimpleIdentityServer.Host
{
    public class Startup
    {
        #region Properties

        public IConfigurationRoot Configuration { get; set; }

        #endregion

        #region Public methods

        public Startup(IHostingEnvironment env,
            IApplicationEnvironment appEnv)
        {
            // Load all the configuration information from the "json" file & the environment variables.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Purpose of this method is to setup the dependency injection
            // IServiceCollection interface is responsible for learning about any services that will be supplied to the running application

            services.AddSimpleIdentityServerCore();

            services.AddLogging();
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            // Specify what frameworks will be used : including MVC, API
            app.UseIISPlatformHandler(options => options.AuthenticationDescriptions.Clear());

            app.UseCookieAuthentication(opt => {
                opt.AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}");
            });
        }

        #endregion

        #region Public static methods

        // Entry point for the application.
        public static void Main(string[] args) => Microsoft.AspNet.Hosting.WebApplication.Run<Startup>(args);

        #endregion
    }
}
