﻿#region copyright
// Copyright 2015 Habart Thierry
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
#endregion

using Moq;
using SimpleIdentityServer.Core.Api.Registration.Actions;
using SimpleIdentityServer.Core.Parameters;
using SimpleIdentityServer.Manager.Core.Api.Clients;
using SimpleIdentityServer.Manager.Core.Api.Clients.Actions;
using SimpleIdentityServer.Manager.Core.Parameters;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SimpleIdentityServer.Manager.Core.Tests.Api.Clients
{
    public class ClientActionsFixture
    {
        private Mock<IGetClientsAction> _getClientsActionStub;
        private Mock<IGetClientAction> _getClientActionStub;
        private Mock<IRemoveClientAction> _removeClientActionStub;
        private Mock<IUpdateClientAction> _updateClientActionStub;
        private Mock<IRegisterClientAction> _registerClientActionStub;
        private Mock<ISearchClientsAction> _searchClientsStub;

        private IClientActions _clientActions;

        [Fact]
        public async Task When_Executing_GetClients_Then_Operation_Is_Called()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT
            await _clientActions.GetClients();

            // ASSERT
            _getClientsActionStub.Verify(g => g.Execute());
        }

        [Fact]
        public async Task When_Executing_GetClient_Then_Operation_Is_Called()
        {
            // ARRANGE
            const string clientId = "clientId";
            InitializeFakeObjects();

            // ACT
            await _clientActions.GetClient(clientId);

            // ASSERT
            _getClientActionStub.Verify(g => g.Execute(clientId));
        }

        [Fact]
        public async Task When_Executing_DeleteClient_Then_Operation_Is_Called()
        {
            // ARRANGE
            const string clientId = "clientId";
            InitializeFakeObjects();

            // ACT
            await _clientActions.DeleteClient(clientId);

            // ASSERT
            _removeClientActionStub.Verify(g => g.Execute(clientId));
        }

        [Fact]
        public async Task When_Executing_UpdateClient_Then_Operation_Is_Called()
        {
            // ARRANGE
            var parameter = new UpdateClientParameter
            {
                ClientId = "client_id"
            };
            InitializeFakeObjects();

            // ACT
            await _clientActions.UpdateClient(parameter);

            // ASSERT
            _updateClientActionStub.Verify(g => g.Execute(parameter));
        }

        [Fact]
        public async Task When_RegisterClient_Then_Operation_Is_Called()
        {
            // ARRANGE
            var parameter = new RegistrationParameter
            {
                RedirectUris = new List<string>
                {
                    "https://localhost/callback"
                }
            };
            InitializeFakeObjects();

            // ACT
            await _clientActions.AddClient(parameter);

            // ASSERT
            _registerClientActionStub.Verify(g => g.Execute(parameter));
        }

        private void InitializeFakeObjects()
        {
            _getClientsActionStub = new Mock<IGetClientsAction>();
            _getClientActionStub = new Mock<IGetClientAction>();
            _removeClientActionStub = new Mock<IRemoveClientAction>();
            _updateClientActionStub = new Mock<IUpdateClientAction>();
            _registerClientActionStub = new Mock<IRegisterClientAction>();
            _searchClientsStub = new Mock<ISearchClientsAction>();
            _clientActions = new ClientActions(_searchClientsStub.Object, _getClientsActionStub.Object,
                _getClientActionStub.Object,
                _removeClientActionStub.Object,
                _updateClientActionStub.Object,
                _registerClientActionStub.Object);
        }
    }
}
