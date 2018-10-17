﻿namespace SimpleIdentityServer.Core.Api.Profile.Actions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Common.Models;

    public interface IGetUserProfilesAction
    {
        Task<IEnumerable<ResourceOwnerProfile>> Execute(string subject);
    }
}