﻿namespace SimpleAuth.AuthServerPg
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication.OAuth;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    internal class ConfigureOAuthOptions : IPostConfigureOptions<OAuthOptions>
    {
        private readonly IConfiguration _configuration;

        public ConfigureOAuthOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public void PostConfigure(string name, OAuthOptions options)
        {
            options.AuthorizationEndpoint = _configuration["OAUTH:AUTHORITY"] + "/authorization";
            options.TokenEndpoint = _configuration["OAUTH:AUTHORITY"] + "/token";
            options.UserInformationEndpoint = _configuration["OAUTH:AUTHORITY"] + "/userinfo";
            options.UsePkce = true;
            options.CallbackPath = "/callback";
            options.Events = new OAuthEvents
            {
                OnCreatingTicket = ctx =>
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(ctx.AccessToken);
                    var claims = jwt.Claims.Where(c => !ctx.Identity.HasClaim(x => x.Type == c.Type)).ToArray();
                    ctx.Identity.AddClaims(claims);
                    ctx.Success();
                    return Task.CompletedTask;
                },
                OnTicketReceived = ctx => Task.CompletedTask
            };
            options.SaveTokens = true;
            options.ClientId = _configuration["OAUTH:CLIENTID"];
            options.ClientSecret = _configuration["OAUTH:CLIENTSECRET"];
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("uma_protection");
        }
    }
}