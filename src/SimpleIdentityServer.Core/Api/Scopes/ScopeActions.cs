﻿// Copyright 2015 Habart Thierry
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace SimpleIdentityServer.Core.Api.Scopes
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Actions;
    using Shared.Models;
    using Shared.Parameters;
    using Shared.Results;

    public interface IScopeActions
    {
        Task<bool> DeleteScope(string scopeName);
        Task<Scope> GetScope(string scopeName);
        Task<ICollection<Scope>> GetScopes();
        Task<bool> AddScope(Scope scope);
        Task<bool> UpdateScope(Scope scope);
        Task<SearchScopeResult> Search(SearchScopesParameter parameter);
    }

    internal class ScopeActions : IScopeActions
    {
        private readonly IDeleteScopeOperation _deleteScopeOperation;
        private readonly IGetScopeOperation _getScopeOperation;
        private readonly IGetScopesOperation _getScopesOperation;
        private readonly IAddScopeOperation _addScopeOperation;
        private readonly IUpdateScopeOperation _updateScopeOperation;
        private readonly ISearchScopesOperation _searchScopesOperation;

        public ScopeActions(
            IDeleteScopeOperation deleteScopeOperation,
            IGetScopeOperation getScopeOperation,
            IGetScopesOperation getScopesOperation,
            IAddScopeOperation addScopeOperation,
            IUpdateScopeOperation updateScopeOperation,
            ISearchScopesOperation searchScopesOperation)
        {
            _deleteScopeOperation = deleteScopeOperation;
            _getScopeOperation = getScopeOperation;
            _getScopesOperation = getScopesOperation;
            _addScopeOperation = addScopeOperation;
            _updateScopeOperation = updateScopeOperation;
            _searchScopesOperation = searchScopesOperation;
        }

        public Task<SearchScopeResult> Search(SearchScopesParameter parameter)
        {
            return _searchScopesOperation.Execute(parameter);
        }

        public Task<bool> DeleteScope(string scopeName)
        {
            return _deleteScopeOperation.Execute(scopeName);
        }

        public Task<Scope> GetScope(string scopeName)
        {
            return _getScopeOperation.Execute(scopeName);
        }

        public Task<ICollection<Scope>> GetScopes()
        {
            return _getScopesOperation.Execute();
        }

        public Task<bool> AddScope(Scope scope)
        {
            return _addScopeOperation.Execute(scope);
        }

        public Task<bool> UpdateScope(Scope scope)
        {
            return _updateScopeOperation.Execute(scope);
        }
    }
}
