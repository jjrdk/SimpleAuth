﻿#region copyright
// Copyright 2016 Habart Thierry
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

using Moq;
using SimpleIdentityServer.Client;
using SimpleIdentityServer.Client.Builders;
using SimpleIdentityServer.Client.Operations;
using SimpleIdentityServer.Client.Selectors;
using SimpleIdentityServer.Common.Client.Factories;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace SimpleIdentityServer.Host.Tests
{
    public class RevokeTokenClientFixture : IClassFixture<TestOauthServerFixture>
    {
        private const string baseUrl = "http://localhost:5000";
        private readonly TestOauthServerFixture _server;
        private Mock<IHttpClientFactory> _httpClientFactoryStub;
        private IClientAuthSelector _clientAuthSelector;

        public RevokeTokenClientFixture(TestOauthServerFixture server)
        {
            _server = server;
        }
        
        [Fact]
        public async Task When_Revoking_AccessToken_Then_True_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();
            _httpClientFactoryStub.Setup(h => h.GetHttpClient()).Returns(_server.Client);

            // ACT
            var result = await _clientAuthSelector.UseClientSecretPostAuth("client", "client")
                .UsePassword("administrator", "password", "scim")
                .ResolveAsync(baseUrl + "/.well-known/openid-configuration");
            var revoke = await _clientAuthSelector.UseClientSecretPostAuth("client", "client")
                .RevokeToken(result.Content.AccessToken, TokenType.AccessToken)
                .ResolveAsync(baseUrl + "/.well-known/openid-configuration");

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _clientAuthSelector.UseClientSecretPostAuth("client", "client")
                .Introspect(result.Content.AccessToken, TokenType.AccessToken)
                .ResolveAsync(baseUrl + "/.well-known/openid-configuration"));

            // ASSERT
            Assert.True(revoke);
            Assert.NotNull(ex);
        }

        [Fact]
        public async Task When_Revoking_RefreshToken_Then_True_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();
            _httpClientFactoryStub.Setup(h => h.GetHttpClient()).Returns(_server.Client);

            // ACT
            var result = await _clientAuthSelector.UseClientSecretPostAuth("client", "client")
                .UsePassword("administrator", "password", "scim")
                .ResolveAsync(baseUrl + "/.well-known/openid-configuration");
            var revoke = await _clientAuthSelector.UseClientSecretPostAuth("client", "client")
                .RevokeToken(result.Content.RefreshToken, TokenType.RefreshToken)
                .ResolveAsync(baseUrl + "/.well-known/openid-configuration");
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _clientAuthSelector.UseClientSecretPostAuth("client", "client")
                .Introspect(result.Content.RefreshToken, TokenType.RefreshToken)
                .ResolveAsync(baseUrl + "/.well-known/openid-configuration"));

            // ASSERT
            Assert.True(revoke);
            Assert.NotNull(ex);
        }

        private void InitializeFakeObjects()
        {
            _httpClientFactoryStub = new Mock<IHttpClientFactory>();
            var requestBuilder = new RequestBuilder();
            var postTokenOperation = new PostTokenOperation(_httpClientFactoryStub.Object);
            var getDiscoveryOperation = new GetDiscoveryOperation(_httpClientFactoryStub.Object);
            var introspectionOperation = new IntrospectOperation(_httpClientFactoryStub.Object);
            var revokeTokenOperation = new RevokeTokenOperation(_httpClientFactoryStub.Object);
            _clientAuthSelector = new ClientAuthSelector(
                new TokenClientFactory(postTokenOperation, getDiscoveryOperation),
                new IntrospectClientFactory(introspectionOperation, getDiscoveryOperation),
                new RevokeTokenClientFactory(revokeTokenOperation, getDiscoveryOperation));
        }
    }
}
