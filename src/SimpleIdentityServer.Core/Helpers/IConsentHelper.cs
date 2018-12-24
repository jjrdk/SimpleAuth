﻿namespace SimpleIdentityServer.Core.Helpers
{
    using System.Threading.Tasks;
    using Parameters;
    using SimpleAuth.Shared.Models;

    public interface IConsentHelper
    {
        Task<Consent> GetConfirmedConsentsAsync(string subject, AuthorizationParameter authorizationParameter);
    }
}