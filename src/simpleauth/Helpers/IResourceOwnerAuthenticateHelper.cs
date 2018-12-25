﻿namespace SimpleAuth.Helpers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Shared.Models;

    public interface IResourceOwnerAuthenticateHelper
    {
        Task<ResourceOwner> Authenticate(string login, string password, IEnumerable<string> exceptedAmrValues = null);
        IEnumerable<string> GetAmrs();
    }
}