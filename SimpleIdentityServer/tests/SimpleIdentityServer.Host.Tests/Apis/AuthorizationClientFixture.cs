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
using Newtonsoft.Json;
using SimpleIdentityServer.Client;
using SimpleIdentityServer.Client.Builders;
using SimpleIdentityServer.Client.Operations;
using SimpleIdentityServer.Client.Selectors;
using SimpleIdentityServer.Common.Client.Factories;
using SimpleIdentityServer.Core.Common;
using SimpleIdentityServer.Core.Common.DTOs.Requests;
using SimpleIdentityServer.Core.Common.DTOs.Responses;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SimpleIdentityServer.Host.Tests
{
    public class AuthorizationClientFixture : IClassFixture<TestOauthServerFixture>
    {
        const string baseUrl = "http://localhost:5000";
        private readonly TestOauthServerFixture _server;
        private Mock<IHttpClientFactory> _httpClientFactoryStub;
        private IAuthorizationClient _authorizationClient;
        private IClientAuthSelector _clientAuthSelector;

        public AuthorizationClientFixture(TestOauthServerFixture server)
        {
            _server = server;
        }

        #region Errors

        [Fact]
        public async Task When_Scope_IsNot_Passed_To_Authorization_Then_Json_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT
            var httpResult = await _server.Client.GetAsync(new Uri(baseUrl + "/authorization" ));
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var error = JsonConvert.DeserializeObject<ErrorResponseWithState>(json);

            // ASSERT
            Assert.NotNull(error);
            Assert.Equal("invalid_request", error.Error);
            Assert.Equal("the parameter scope is missing", error.ErrorDescription);
        }

        [Fact]
        public async Task When_ClientId_IsNot_Passed_To_Authorization_Then_Json_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT
            var httpResult = await _server.Client.GetAsync(new Uri(baseUrl + "/authorization?scope=scope"));
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var error = JsonConvert.DeserializeObject<ErrorResponseWithState>(json);

            // ASSERT
            Assert.NotNull(error);
            Assert.Equal("invalid_request", error.Error);
            Assert.Equal("the parameter client_id is missing", error.ErrorDescription);
        }

        [Fact]
        public async Task When_RedirectUri_IsNot_Passed_To_Authorization_Then_Json_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT
            var httpResult = await _server.Client.GetAsync(new Uri(baseUrl + "/authorization?scope=scope&client_id=client"));
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var error = JsonConvert.DeserializeObject<ErrorResponseWithState>(json);

            // ASSERT
            Assert.NotNull(error);
            Assert.Equal("invalid_request", error.Error);
            Assert.Equal("the parameter redirect_uri is missing", error.ErrorDescription);
        }

        [Fact]
        public async Task When_ResponseType_IsNot_Passed_To_Authorization_Then_Json_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT
            var httpResult = await _server.Client.GetAsync(new Uri(baseUrl + "/authorization?scope=scope&client_id=client&redirect_uri=redirect_uri"));
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var error = JsonConvert.DeserializeObject<ErrorResponseWithState>(json);

            // ASSERT
            Assert.NotNull(error);
            Assert.Equal("invalid_request", error.Error);
            Assert.Equal("the parameter response_type is missing", error.ErrorDescription);
        }

        [Fact]
        public async Task When_Unsupported_ResponseType_Is_Passed_To_Authorization_Then_Json_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT
            var httpResult = await _server.Client.GetAsync(new Uri(baseUrl + "/authorization?scope=scope&state=state&client_id=client&redirect_uri=redirect_uri&response_type=invalid"));
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var error = JsonConvert.DeserializeObject<ErrorResponseWithState>(json);

            // ASSERT
            Assert.NotNull(error);
            Assert.Equal("invalid_request", error.Error);
            Assert.Equal("at least one response_type parameter is not supported", error.ErrorDescription);
            Assert.Equal("state", error.State);
        }

        [Fact]
        public async Task When_UnsupportedPrompt_Is_Passed_To_Authorization_Then_Json_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT
            var httpResult = await _server.Client.GetAsync(new Uri(baseUrl + "/authorization?scope=scope&state=state&client_id=client&redirect_uri=redirect_uri&response_type=token&prompt=invalid"));
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var error = JsonConvert.DeserializeObject<ErrorResponseWithState>(json);

            // ASSERT
            Assert.NotNull(error);
            Assert.Equal("invalid_request", error.Error);
            Assert.Equal("at least one prompt parameter is not supported", error.ErrorDescription);
            Assert.Equal("state", error.State);

        }

        [Fact]
        public async Task When_Not_Correct_Redirect_Uri_Is_Passed_To_Authorization_Then_Json_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT
            var httpResult = await _server.Client.GetAsync(new Uri(baseUrl + "/authorization?scope=scope&state=state&client_id=client&redirect_uri=redirect_uri&response_type=token&prompt=none"));
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var error = JsonConvert.DeserializeObject<ErrorResponseWithState>(json);

            // ASSERT
            Assert.NotNull(error);
            Assert.Equal("invalid_request", error.Error);
            Assert.Equal("Based on the RFC-3986 the redirection-uri is not well formed", error.ErrorDescription);
            Assert.Equal("state", error.State);
        }

        [Fact]
        public async Task When_Not_Correct_ClientId_Is_Passed_To_Authorization_Then_Json_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();


            // ACT
            var httpResult = await _server.Client.GetAsync(new Uri(baseUrl + "/authorization?scope=scope&state=state&client_id=bad_client&redirect_uri=http://localhost:5000&response_type=token&prompt=none"));
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var error = JsonConvert.DeserializeObject<ErrorResponseWithState>(json);

            // ASSERT
            Assert.NotNull(error);
            Assert.Equal("invalid_request", error.Error);
            Assert.Equal("the client id parameter bad_client doesn't exist or is not valid", error.ErrorDescription);
            Assert.Equal("state", error.State);
        }

        [Fact]
        public async Task When_Not_Support_Redirect_Uri_Is_Passed_To_Authorization_Then_Json_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();


            // ACT
            var httpResult = await _server.Client.GetAsync(new Uri(baseUrl + "/authorization?scope=scope&state=state&client_id=pkce_client&redirect_uri=http://localhost:5000&response_type=token&prompt=none"));
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var error = JsonConvert.DeserializeObject<ErrorResponseWithState>(json);

            // ASSERT
            Assert.NotNull(error);
            Assert.Equal("invalid_request", error.Error);
            Assert.Equal("the redirect url http://localhost:5000 doesn't exist or is not valid", error.ErrorDescription);
            Assert.Equal("state", error.State);
        }

        #endregion

        [Fact]
        public async Task When_Requesting_Token_And_CodeVerifier_Is_Passed_Then_Token_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();
            _httpClientFactoryStub.Setup(h => h.GetHttpClient()).Returns(_server.Client);
            var builder = new PkceBuilder();
            var pkce = builder.Build(CodeChallengeMethods.S256);

            // ACT
            var result = await _authorizationClient.ResolveAsync(baseUrl + "/.well-known/openid-configuration", new AuthorizationRequest(
                new[] { "openid", "api1" },
                new[] { ResponseTypes.Code },
                "pkce_client", "http://localhost:5000/callback",
                "state")
            {
                CodeChallenge = pkce.CodeChallenge,
                CodeChallengeMethod = CodeChallengeMethods.S256,
                Prompt = PromptNames.None
            });
            Uri location = result.Location;
            var queries = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(location.Query);
            var token = await _clientAuthSelector.UseClientSecretPostAuth("pkce_client", "pkce_client")
                .UseAuthorizationCode(queries["code"], "http://localhost:5000/callback", pkce.CodeVerifier)
                .ResolveAsync(baseUrl + "/.well-known/openid-configuration");

            // ASSERT
            Assert.NotNull(token);
            Assert.NotEmpty(token.Content.AccessToken);
        }

        [Fact]
        public async Task When_Requesting_AuthorizationCode_And_RedirectUri_IsNotValid_Then_Error_Is_Returned()
        {
            const string baseUrl = "http://localhost:5000";
            // ARRANGE
            InitializeFakeObjects();
            _httpClientFactoryStub.Setup(h => h.GetHttpClient()).Returns(_server.Client);

            // ACT
            var result = await _authorizationClient.ResolveAsync(baseUrl + "/.well-known/openid-configuration", new AuthorizationRequest(new[] { "openid", "api1" }, new[] { ResponseTypes.Code }, "implicit_client", "http://localhost:5000/invalid_callback", "state"));
            
            // ASSERTS
            Assert.NotNull(result);
            Assert.True(result.ContainsError);
            Assert.True(result.Error.Error == "invalid_request");
        }

        [Fact]
        public async Task When_Requesting_AuthorizationCode_Then_Code_Is_Returned()
        {
            const string baseUrl = "http://localhost:5000";
            // ARRANGE
            InitializeFakeObjects();
            _httpClientFactoryStub.Setup(h => h.GetHttpClient()).Returns(_server.Client);

            // ACT
            // NOTE : The consent has already been given in the database.
            var result = await _authorizationClient.ResolveAsync(baseUrl + "/.well-known/openid-configuration", 
                new AuthorizationRequest(new[] { "openid", "api1" }, new[] { ResponseTypes.Code }, "authcode_client", "http://localhost:5000/callback", "state")
                {
                    Prompt = PromptNames.None
                });
            Uri location = result.Location;
            var queries = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(location.Query);
            var token = await _clientAuthSelector.UseClientSecretPostAuth("authcode_client", "authcode_client")
                .UseAuthorizationCode(queries["code"], "http://localhost:5000/callback")
                .ResolveAsync(baseUrl + "/.well-known/openid-configuration");

            // ASSERTS
            Assert.NotNull(result);
            Assert.NotNull(result.Location);
            Assert.NotNull(token);
            Assert.NotEmpty(token.Content.AccessToken);
            Assert.True(queries["state"] == "state");
        }

        [Fact]
        public async Task When_Requesting_IdTokenAndAccessToken_Then_Tokens_Are_Returned()
        {
            const string baseUrl = "http://localhost:5000";
            // ARRANGE
            InitializeFakeObjects();
            _httpClientFactoryStub.Setup(h => h.GetHttpClient()).Returns(_server.Client);

            // ACT
            // NOTE : The consent has already been given in the database.
            var result = await _authorizationClient.ResolveAsync(baseUrl + "/.well-known/openid-configuration",
                new AuthorizationRequest(new[] { "openid", "api1" }, new[] { ResponseTypes.IdToken, ResponseTypes.Token }, "implicit_client", "http://localhost:5000/callback", "state")
                {
                    Prompt = PromptNames.None,
                    Nonce = "nonce"
                });
            var queries = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(result.Location.Fragment.TrimStart('#'));

            // ASSERTS
            Assert.NotNull(result);
            Assert.NotNull(result.Location);
            Assert.True(queries.ContainsKey("id_token"));
            Assert.True(queries.ContainsKey("access_token"));
            Assert.True(queries.ContainsKey("state"));
            Assert.True(queries["state"] == "state");
        }

        [Fact]
        public async Task When_RequestingIdTokenAndAuthorizationCodeAndAccessToken_Then_Tokens_Are_Returned()
        {
            const string baseUrl = "http://localhost:5000";
            // ARRANGE
            InitializeFakeObjects();
            _httpClientFactoryStub.Setup(h => h.GetHttpClient()).Returns(_server.Client);

            // ACT
            // NOTE : The consent has already been given in the database.
            var result = await _authorizationClient.ResolveAsync(baseUrl + "/.well-known/openid-configuration",
                new AuthorizationRequest(new[] { "openid", "api1" }, new[] { ResponseTypes.IdToken, ResponseTypes.Token, ResponseTypes.Code }, "hybrid_client", "http://localhost:5000/callback", "state")
                {
                    Prompt = PromptNames.None,
                    Nonce = "nonce"
                });
            var queries = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(result.Location.Fragment.TrimStart('#'));

            // ASSERTS
            Assert.NotNull(result);
            Assert.NotNull(result.Location);
            Assert.True(queries.ContainsKey("id_token"));
            Assert.True(queries.ContainsKey("access_token"));
            Assert.True(queries.ContainsKey("code"));
            Assert.True(queries.ContainsKey("state"));
            Assert.True(queries["state"] == "state");
        }

        private void InitializeFakeObjects()
        {
            _httpClientFactoryStub = new Mock<IHttpClientFactory>();
            var getAuthorizationOperation = new GetAuthorizationOperation(_httpClientFactoryStub.Object);
            var getDiscoveryOperation = new GetDiscoveryOperation(_httpClientFactoryStub.Object);
            var postTokenOperation = new PostTokenOperation(_httpClientFactoryStub.Object);
            var introspectionOperation = new IntrospectOperation(_httpClientFactoryStub.Object);
            var revokeTokenOperation = new RevokeTokenOperation(_httpClientFactoryStub.Object);
            _authorizationClient = new AuthorizationClient(getAuthorizationOperation, getDiscoveryOperation);
            _clientAuthSelector = new ClientAuthSelector(
                new TokenClientFactory(postTokenOperation, getDiscoveryOperation),
                new IntrospectClientFactory(introspectionOperation, getDiscoveryOperation),
                new RevokeTokenClientFactory(revokeTokenOperation, getDiscoveryOperation));
        }
    }
}
