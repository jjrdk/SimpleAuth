﻿namespace SimpleIdentityServer.Core.WebSite.User.Actions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using SimpleAuth.Shared.Models;

    public interface IUpdateUserClaimsOperation
    {
        Task<bool> Execute(string subject, IEnumerable<ClaimAggregate> claims);
    }
}