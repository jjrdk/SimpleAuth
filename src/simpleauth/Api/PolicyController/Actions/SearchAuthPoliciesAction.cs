﻿namespace SimpleAuth.Api.PolicyController.Actions
{
    using System;
    using System.Threading.Tasks;
    using Parameters;
    using Repositories;
    using Shared.Models;

    internal sealed class SearchAuthPoliciesAction : ISearchAuthPoliciesAction
    {
        private readonly IPolicyRepository _policyRepository;

        public SearchAuthPoliciesAction(IPolicyRepository policyRepository)
        {
            _policyRepository = policyRepository;
        }

        public Task<SearchAuthPoliciesResult> Execute(SearchAuthPoliciesParameter parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            return _policyRepository.Search(parameter);
        }
    }
}
