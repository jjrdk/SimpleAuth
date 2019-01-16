﻿namespace SimpleAuth.OAuth2Introspection
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Client;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Shared;

    public class OAuth2IntrospectionHandler : AuthenticationHandler<OAuth2IntrospectionOptions>
    {
        private readonly IIntrospectClient _client;

        public OAuth2IntrospectionHandler(IOptionsMonitor<OAuth2IntrospectionOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IIntrospectClient client) : base(options, logger, encoder, clock)
        {
            _client = client;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string authorization = Request.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(authorization))
            {
                return AuthenticateResult.NoResult();
            }

            string token = null;
            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authorization.Substring("Bearer ".Length).Trim();
            }

            if (string.IsNullOrEmpty(token))
            {
                return AuthenticateResult.NoResult();
            }

            try
            {
                var introspectionResult = await _client
                    .ResolveAsync(Options.WellKnownConfigurationUrl)
                    .ConfigureAwait(false);
                if (introspectionResult.ContainsError || !introspectionResult.Content.Active)
                {
                    return AuthenticateResult.NoResult();
                }

                var claims = new List<Claim>
                {
                    new Claim(StandardClaimNames.ExpirationTime, introspectionResult.Content.Expiration.ToString()),
                    new Claim(StandardClaimNames.Iat,
                        introspectionResult.Content.IssuedAt.ToString(CultureInfo.InvariantCulture))
                };

                if (!string.IsNullOrWhiteSpace(introspectionResult.Content.Subject))
                {
                    claims.Add(new Claim(JwtConstants.StandardResourceOwnerClaimNames.Subject,
                        introspectionResult.Content.Subject));
                }

                if (!string.IsNullOrWhiteSpace(introspectionResult.Content.ClientId))
                {
                    claims.Add(new Claim(StandardClaimNames.ClientId, introspectionResult.Content.ClientId));
                }

                if (!string.IsNullOrWhiteSpace(introspectionResult.Content.Issuer))
                {
                    claims.Add(new Claim(StandardClaimNames.Issuer, introspectionResult.Content.Issuer));
                }

                if (introspectionResult.Content.Scope != null)
                {
                    foreach (var scope in introspectionResult.Content.Scope)
                    {
                        claims.Add(new Claim(StandardClaimNames.Scopes, scope));
                    }
                }

                var claimsIdentity = new ClaimsIdentity(claims, OAuth2IntrospectionOptions.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                var authenticationTicket = new AuthenticationTicket(
                    claimsPrincipal,
                    new AuthenticationProperties(),
                    OAuth2IntrospectionOptions.AuthenticationScheme);
                return AuthenticateResult.Success(authenticationTicket);
            }
            catch (Exception)
            {
                return AuthenticateResult.NoResult();
            }
        }
    }
}
