﻿namespace SimpleAuth.AcceptanceTests.Features
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using Microsoft.IdentityModel.Tokens;
    using SimpleAuth.Client;
    using SimpleAuth.Shared.Responses;
    using Xbehave;
    using Xunit;

    public class IdTokenSigningFeature : AuthFlowFeature
    {
        [Scenario]
        public void WhenClientHasNoSigningKeysThenUsesServerKey()
        {
            TokenClient client = null;
            GrantedTokenResponse token = null;

            "Given a token client".x(async () =>
            {
                client = await TokenClient.Create(
                    TokenCredentials.FromClientCredentials("no_key", "no_key"),
                    _fixture.Client,
                    new Uri(WellKnownOpenidConfiguration)).ConfigureAwait(false);
            });

            "When getting token".x(async () =>
            {
                var response = await client
                    .GetToken(TokenRequest.FromPassword("administrator", "password", new[] { "api" }))
                    .ConfigureAwait(false);
                token = response.Content;

                Assert.NotNull(token);
            });

            "Then token is signed with server key".x(() =>
            {
                var key = _jwks.GetSignKeys().First();
                var validationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = key,
                    ValidateAudience = false,
                    ValidateActor = false,
                    ValidateIssuer = false,
                    ValidateLifetime = false,
                    ValidateTokenReplay = false
                };
                var handler = new JwtSecurityTokenHandler();
                handler.ValidateToken(token.IdToken, validationParameters, out _);
            });
        }
    }
}