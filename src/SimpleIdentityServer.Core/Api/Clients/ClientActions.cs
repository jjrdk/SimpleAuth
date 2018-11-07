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

namespace SimpleIdentityServer.Core.Api.Clients
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Actions;
    using Parameters;
    using Registration.Actions;
    using Shared.Models;
    using Shared.Parameters;
    using Shared.Responses;
    using Shared.Results;

    public interface IClientActions
    {
        Task<SearchClientResult> Search(SearchClientParameter parameter);
        Task<IEnumerable<Client>> GetClients();
        Task<Client> GetClient(string clientId);
        Task<bool> DeleteClient(string clientId);
        Task<bool> UpdateClient(UpdateClientParameter updateClientParameter);
        Task<ClientRegistrationResponse> AddClient(RegistrationParameter registrationParameter);
    }

    public class ClientActions : IClientActions
    {
        private readonly ISearchClientsAction _searchClientsAction;
        private readonly IGetClientsAction _getClientsAction;
        private readonly IGetClientAction _getClientAction;
        private readonly IRemoveClientAction _removeClientAction;
        private readonly IUpdateClientAction _updateClientAction;
        private readonly IRegisterClientAction _registerClientAction;

        public ClientActions(
            ISearchClientsAction searchClientsAction,
            IGetClientsAction getClientsAction,
            IGetClientAction getClientAction,
            IRemoveClientAction removeClientAction,
            IUpdateClientAction updateClientAction,
            IRegisterClientAction registerClientAction)
        {
            _searchClientsAction = searchClientsAction;
            _getClientsAction = getClientsAction;
            _getClientAction = getClientAction;
            _removeClientAction = removeClientAction;
            _updateClientAction = updateClientAction;
            _registerClientAction = registerClientAction;
        }

        public Task<SearchClientResult> Search(SearchClientParameter parameter)
        {
            return _searchClientsAction.Execute(parameter);
        }
        
        public Task<IEnumerable<Client>> GetClients()
        {
            return _getClientsAction.Execute();
        }

        public Task<Client> GetClient(string clientId)
        {
            return _getClientAction.Execute(clientId);
        }

        public Task<bool> DeleteClient(string clientId)
        {
            return _removeClientAction.Execute(clientId);
        }

        public Task<bool> UpdateClient(UpdateClientParameter updateClientParameter)
        {
            return _updateClientAction.Execute(updateClientParameter);
        }

        public Task<ClientRegistrationResponse> AddClient(RegistrationParameter registrationParameter)
        {
            return _registerClientAction.Execute(registrationParameter);
        }
    }
}
