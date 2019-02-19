﻿// Copyright © 2016 Habart Thierry, © 2018 Jacob Reimers
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

namespace SimpleAuth.Server.Tests.Apis
{
    using Client;
    using Microsoft.IdentityModel.Logging;
    using Newtonsoft.Json;
    using Shared;
    using Shared.Responses;
    using SimpleAuth.Shared.Errors;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Xunit;

    public class RevokeTokenClientFixture
    {
        private const string BaseUrl = "http://localhost:5000";
        private const string WellKnownOpenidConfiguration = "/.well-known/openid-configuration";
        private readonly TestOauthServerFixture _server;

        public RevokeTokenClientFixture()
        {
            IdentityModelEventSource.ShowPII = true;
            _server = new TestOauthServerFixture();
        }

        [Fact]
        public async Task When_No_Parameters_Is_Passed_To_TokenRevoke_Edp_Then_Error_Is_Returned()
        {
            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{BaseUrl}/token/revoke")
            };

            var httpResult = await _server.Client.SendAsync(httpRequest).ConfigureAwait(false);
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.BadRequest, httpResult.StatusCode);
            var error = JsonConvert.DeserializeObject<ErrorResponse>(json);
            Assert.NotNull(error);
            Assert.Equal(ErrorCodes.InvalidRequestCode, error.Error);
            Assert.Equal("the parameter token is missing", error.ErrorDescription);
        }

        [Fact]
        public async Task When_No_Valid_Parameters_Is_Passed_Then_Error_Is_Returned()
        {
            var request = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("invalid", "invalid")
            };
            var body = new FormUrlEncodedContent(request);
            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = body,
                RequestUri = new Uri($"{BaseUrl}/token/revoke")
            };

            var httpResult = await _server.Client.SendAsync(httpRequest).ConfigureAwait(false);
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);

            var error = JsonConvert.DeserializeObject<ErrorResponse>(json);
            Assert.NotNull(error);
            Assert.Equal(ErrorCodes.InvalidRequestCode, error.Error);
            Assert.Equal("the parameter token is missing", error.ErrorDescription);
        }

        [Fact]
        public async Task When_Revoke_Token_And_Client_Cannot_Be_Authenticated_Then_Error_Is_Returned()
        {
            var tokenClient = await TokenClient.Create(
                    TokenCredentials.FromClientCredentials("invalid_client", "invalid_client"),
                    _server.Client,
                    new Uri(BaseUrl + WellKnownOpenidConfiguration))
                .ConfigureAwait(false);
            var ex = await tokenClient
                .RevokeToken(RevokeTokenRequest.RevokeToken("access_token", TokenTypes.AccessToken))
                .ConfigureAwait(false);

            Assert.True(ex.ContainsError);
            Assert.Equal("invalid_client", ex.Error.Error);
            Assert.Equal("the client doesn't exist", ex.Error.ErrorDescription);
        }

        [Fact]
        public async Task When_Token_Does_Not_Exist_Then_Error_Is_Returned()
        {
            var tokenClient = await TokenClient.Create(
                    TokenCredentials.FromClientCredentials("client", "client"),
                    _server.Client,
                    new Uri(BaseUrl + WellKnownOpenidConfiguration))
                .ConfigureAwait(false);
            var ex = await tokenClient
                .RevokeToken(RevokeTokenRequest.RevokeToken("access_token", TokenTypes.AccessToken))
                .ConfigureAwait(false);

            Assert.True(ex.ContainsError);
            Assert.Equal("invalid_token", ex.Error.Error);
            Assert.Equal("the token doesn't exist", ex.Error.ErrorDescription);
        }

        [Fact]
        public async Task When_Revoke_Token_And_Client_Is_Different_Then_Error_Is_Returned()
        {
            var tokenClient = await TokenClient.Create(
                    TokenCredentials.FromClientCredentials("client_userinfo_enc_rsa15", "client_userinfo_enc_rsa15"),
                    _server.Client,
                    new Uri(BaseUrl + WellKnownOpenidConfiguration))
                .ConfigureAwait(false);
            var result = await tokenClient
                .GetToken(TokenRequest.FromPassword("administrator", "password", new[] { "scim" }))
                .ConfigureAwait(false);
            var revokeClient = await TokenClient.Create(
                    TokenCredentials.FromClientCredentials("client", "client"),
                    _server.Client,
                    new Uri(BaseUrl + WellKnownOpenidConfiguration))
                .ConfigureAwait(false);
            var ex = await revokeClient
                .RevokeToken(RevokeTokenRequest.RevokeToken(result.Content.AccessToken, TokenTypes.AccessToken))
                .ConfigureAwait(false);

            Assert.True(ex.ContainsError);
            Assert.Equal("invalid_token", ex.Error.Error);
            Assert.Equal("the token has not been issued for the given client id 'client'", ex.Error.ErrorDescription);
        }

        [Fact]
        public async Task When_Revoking_AccessToken_Then_True_Is_Returned()
        {
            var tokenClient = await TokenClient.Create(
                    TokenCredentials.FromClientCredentials("client", "client"),
                    _server.Client,
                    new Uri(BaseUrl + WellKnownOpenidConfiguration))
                .ConfigureAwait(false);
            var result = await tokenClient
                .GetToken(TokenRequest.FromPassword("administrator", "password", new[] { "scim" }))
                .ConfigureAwait(false);
            var revoke = await tokenClient
                .RevokeToken(RevokeTokenRequest.RevokeToken(result.Content.AccessToken, TokenTypes.AccessToken))
                .ConfigureAwait(false);
            var introspectionClient = await IntrospectClient.Create(
                    TokenCredentials.FromClientCredentials("client", "client"),
                    _server.Client,
                    new Uri(BaseUrl + WellKnownOpenidConfiguration))
                .ConfigureAwait(false);
            var ex = await introspectionClient.Introspect(
                    IntrospectionRequest.Create(result.Content.AccessToken, TokenTypes.AccessToken))
                .ConfigureAwait(false);

            Assert.False(revoke.ContainsError);
            Assert.True(ex.ContainsError);
        }

        [Fact]
        public async Task When_Revoking_RefreshToken_Then_True_Is_Returned()
        {
            var tokenClient = await TokenClient.Create(
                    TokenCredentials.FromClientCredentials("client", "client"),
                    _server.Client,
                    new Uri(BaseUrl + WellKnownOpenidConfiguration))
                .ConfigureAwait(false);
            var result = await tokenClient
                .GetToken(TokenRequest.FromPassword("administrator", "password", new[] { "scim" }))
                .ConfigureAwait(false);
            var revoke = await tokenClient
                .RevokeToken(RevokeTokenRequest.RevokeToken(result.Content.RefreshToken, TokenTypes.RefreshToken))
                .ConfigureAwait(false);
            var introspectClient = await IntrospectClient.Create(
                    TokenCredentials.FromClientCredentials("client", "client"),
                    _server.Client,
                    new Uri(BaseUrl + WellKnownOpenidConfiguration))
                .ConfigureAwait(false);
            var ex = await introspectClient.Introspect(
                    IntrospectionRequest.Create(result.Content.RefreshToken, TokenTypes.RefreshToken))
                .ConfigureAwait(false);

            Assert.False(revoke.ContainsError);
            Assert.True(ex.ContainsError);
        }
    }
}
