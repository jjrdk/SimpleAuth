﻿namespace SimpleIdentityServer.Core.Api.Authorization.Actions
{
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Parameters;
    using Results;
    using SimpleAuth.Shared.Models;

    public interface IGetAuthorizationCodeAndTokenViaHybridWorkflowOperation
    {
        Task<EndpointResult> Execute(AuthorizationParameter authorizationParameter, IPrincipal claimsPrincipal, Client client, string issuerName);
    }
}