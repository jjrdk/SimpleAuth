﻿namespace SimpleAuth.Uma.Api.PolicyController.Actions
{
    using System.Threading.Tasks;
    using Models;

    public interface IGetAuthorizationPolicyAction
    {
        Task<Policy> Execute(string policyId);
    }
}