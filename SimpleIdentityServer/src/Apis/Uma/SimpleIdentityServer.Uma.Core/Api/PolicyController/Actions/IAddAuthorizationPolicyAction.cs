﻿namespace SimpleIdentityServer.Uma.Core.Api.PolicyController.Actions
{
    using System.Threading.Tasks;
    using Parameters;

    public interface IAddAuthorizationPolicyAction
    {
        Task<string> Execute(AddPolicyParameter addPolicyParameter);
    }
}