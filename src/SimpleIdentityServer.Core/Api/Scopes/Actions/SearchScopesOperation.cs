﻿namespace SimpleIdentityServer.Core.Api.Scopes.Actions
{
    using System;
    using System.Threading.Tasks;
    using SimpleAuth.Shared.Parameters;
    using SimpleAuth.Shared.Repositories;
    using SimpleAuth.Shared.Results;

    internal sealed class SearchScopesOperation : ISearchScopesOperation
    {
        private readonly IScopeRepository _scopeRepository;

        public SearchScopesOperation(IScopeRepository scopeRepository)
        {
            _scopeRepository = scopeRepository;
        }

        public Task<SearchScopeResult> Execute(SearchScopesParameter parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            return _scopeRepository.Search(parameter);
        }
    }
}
