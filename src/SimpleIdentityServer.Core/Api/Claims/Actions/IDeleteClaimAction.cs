﻿namespace SimpleIdentityServer.Core.Api.Claims.Actions
{
    using System.Threading.Tasks;

    public interface IDeleteClaimAction
    {
        Task<bool> Execute(string claimCode);
    }
}