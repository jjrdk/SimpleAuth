﻿// Copyright © 2018 Habart Thierry, © 2018 Jacob Reimers
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

namespace SimpleAuth.Server.Tests
{
    using System;
    using System.Net.Http;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;

    public class TestOauthServerFixture : IDisposable
    {
        public TestServer Server { get; }
        public HttpClient Client { get; }
        public SharedContext SharedCtx { get; }

        public TestOauthServerFixture()
        {
            SharedCtx = new SharedContext();
            var startup = new FakeStartup(SharedCtx);
            Server = new TestServer(
                new WebHostBuilder()
                .UseUrls("http://localhost:5000")
                .ConfigureServices(startup.ConfigureServices)
                .UseSetting(WebHostDefaults.ApplicationKey, typeof(FakeStartup).Assembly.FullName)
                .Configure(startup.Configure));
            Client = Server.CreateClient();
            SharedCtx.Client = Client;
            SharedCtx.ClientHandler = Server.CreateHandler();
        }

        public void Dispose()
        {
            Server.Dispose();
            Client.Dispose();
        }
    }
}
