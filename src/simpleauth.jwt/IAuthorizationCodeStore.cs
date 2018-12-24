﻿namespace SimpleIdentityServer.Core.Jwt
{
    using System.Threading.Tasks;
    using SimpleAuth.Shared.Models;

    public interface IAuthorizationCodeStore
    {
        Task<AuthorizationCode> GetAuthorizationCode(string code);
        Task<bool> AddAuthorizationCode(AuthorizationCode authorizationCode);
        Task<bool> RemoveAuthorizationCode(string code);
    }
}
