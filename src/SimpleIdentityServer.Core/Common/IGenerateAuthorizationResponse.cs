﻿namespace SimpleIdentityServer.Core.Common
{
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Parameters;
    using Results;
    using SimpleAuth.Shared.Models;

    public interface IGenerateAuthorizationResponse
    {
        Task ExecuteAsync(EndpointResult endpointResult, AuthorizationParameter authorizationParameter, ClaimsPrincipal claimsPrincipal, Client client, string issuerName);
    }
}