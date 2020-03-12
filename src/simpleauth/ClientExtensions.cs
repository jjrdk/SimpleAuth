﻿namespace SimpleAuth
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.IdentityModel.Tokens;
    using Shared.Models;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Repositories;

    internal static class ClientExtensions
    {
        public static async Task<TokenValidationParameters> CreateValidationParameters(this Client client, IJwksStore jwksStore, string audience = null, string issuer = null)
        {
            var signingKeys = client.JsonWebKeys?.Keys.Where(key => key.Use == JsonWebKeyUseNames.Sig)
                .Select(key => new SigningCredentials(key, key.Alg))
                .ToList();
            if (signingKeys?.Count == 0)
            {
                var keys = await (client.IdTokenSignedResponseAlg == null
                    ? jwksStore.GetDefaultSigningKey()
                    : jwksStore.GetSigningKey(client.IdTokenSignedResponseAlg)).ConfigureAwait(false);

                signingKeys = new List<SigningCredentials> { keys };
            }
            var encryptionKeys = client.JsonWebKeys.GetEncryptionKeys().ToArray();
            if (encryptionKeys.Length == 0 && client.IdTokenEncryptedResponseAlg != null)
            {
                var key = await jwksStore.GetEncryptionKey(client.IdTokenEncryptedResponseAlg).ConfigureAwait(false);

                encryptionKeys = new[] { key };
            }
            var parameters = new TokenValidationParameters
            {
                IssuerSigningKeys = signingKeys.Select(x => x.Key).ToList(),
                TokenDecryptionKeys = encryptionKeys
            };
            if (audience != null)
            {
                parameters.ValidAudience = audience;
            }
            else
            {
                parameters.ValidateAudience = false;
            }
            if (issuer != null)
            {
                parameters.ValidIssuer = issuer;
            }
            else
            {
                parameters.ValidateIssuer = false;
            }

            return parameters;
        }
    }
}