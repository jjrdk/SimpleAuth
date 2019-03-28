﻿namespace SimpleAuth.Stores.Marten.AcceptanceTests
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Net.Http;

    public class TestServerFixture : IDisposable
    {
        public TestServer Server { get; }
        public HttpClient Client { get; }
        public SharedContext SharedCtx { get; }

        public TestServerFixture(string connectionString, params string[] urls)
        {
            SharedCtx = SharedContext.Instance;
            //var startup = new ServerStartup(SharedCtx, connectionString);
            Server = new TestServer(
                new WebHostBuilder().UseUrls(urls)
                    .UseConfiguration(
                        new ConfigurationBuilder().AddUserSecrets<ServerStartup>().AddEnvironmentVariables().Build())
                    .ConfigureServices(
                        services =>
                        {
                            services.AddSingleton(SharedCtx);
                            services.AddSingleton<IStartup>(new ServerStartup(SharedCtx, connectionString));
                            //services.AddSingleton<IStartup>(startup);
                        })
                    .UseSetting(WebHostDefaults.ApplicationKey, typeof(ServerStartup).Assembly.FullName));
            Client = Server.CreateClient();
            SharedCtx.Client = Client;
        }

        public void Dispose()
        {
            Server.Dispose();
            Client.Dispose();
        }
    }
}