﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SimpleIdentityServer.Client.DTOs.Response;

namespace SimpleIdentityServer.AccessToken.Store.Redis
{
    internal sealed class RedisTokenStore : IAccessTokenStore
    {
        public Task<GrantedToken> GetToken(string url, string clientId, string clientSecret, IEnumerable<string> scopes)
        {
            return null;
        }
    }
}
