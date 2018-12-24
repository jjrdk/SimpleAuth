﻿namespace SimpleIdentityServer.Core.Api.Claims.Actions
{
    using System.Threading.Tasks;
    using SimpleAuth.Shared.Models;

    public interface IGetClaimAction
    {
        Task<ClaimAggregate> Execute(string claimCode);
    }
}