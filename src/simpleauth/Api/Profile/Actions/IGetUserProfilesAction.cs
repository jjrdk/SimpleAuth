﻿namespace SimpleAuth.Api.Profile.Actions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Shared.Models;

    public interface IGetUserProfilesAction
    {
        Task<IEnumerable<ResourceOwnerProfile>> Execute(string subject);
    }
}