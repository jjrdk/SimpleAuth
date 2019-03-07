﻿// Copyright © 2018 Habart Thierry, © 2018 Jacob Reimers
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

namespace SimpleAuth.Server.Tests
{
    using System;
    using System.Threading.Tasks;
    using SimpleAuth.Server.Tests.MiddleWares;
    using SimpleAuth.Shared.DTOs;
    using SimpleAuth.Shared.Errors;
    using SimpleAuth.Uma.Client;
    using Xunit;

    public class PermissionFixture
    {
        private const string BaseUrl = "http://localhost:5000";
        private const string WellKnownUma2Configuration = "/.well-known/uma2-configuration";

        private readonly UmaClient _umaClient;
        private readonly TestUmaServerFixture _server;

        public PermissionFixture()
        {
            _server = new TestUmaServerFixture();
            _umaClient = UmaClient.Create(_server.Client, new Uri(BaseUrl + WellKnownUma2Configuration)).Result;
        }

        [Fact]
        public async Task When_Client_Is_Not_Authenticated_Then_Error_Is_Returned()
        {
            var resource = await _umaClient.AddResource(
                    new PostResourceSet { Name = "picture", Scopes = new [] { "read" } },
                    "header")
                .ConfigureAwait(false);

            UmaUserStore.Instance().ClientId = null;
            var ticket = await _umaClient.AddPermission(
                    new PostPermission { ResourceSetId = resource.Content.Id, Scopes = new [] { "read" } },
                    "header")
                .ConfigureAwait(false);
            UmaUserStore.Instance().ClientId = "client";

            Assert.True(ticket.ContainsError);
            Assert.Equal(ErrorCodes.InvalidRequestCode, ticket.Error.Error);
            Assert.Equal("the client_id cannot be extracted", ticket.Error.ErrorDescription);
        }

        [Fact]
        public async Task When_ResourceSetId_Is_Null_Then_Error_Is_Returned()
        {
            var ticket = await _umaClient.AddPermission(
                    new PostPermission { ResourceSetId = string.Empty },
                    "header")
                .ConfigureAwait(false);

            Assert.True(ticket.ContainsError);
            Assert.Equal(ErrorCodes.InvalidRequestCode, ticket.Error.Error);
            Assert.Equal("the parameter resource_set_id needs to be specified", ticket.Error.ErrorDescription);
        }

        [Fact]
        public async Task When_Scopes_Is_Null_Then_Error_Is_Returned()
        {
            var ticket = await _umaClient.AddPermission(
                    new PostPermission { ResourceSetId = "resource" },
                    "header")
                .ConfigureAwait(false);

            Assert.True(ticket.ContainsError);
            Assert.Equal(ErrorCodes.InvalidRequestCode, ticket.Error.Error);
            Assert.Equal("the parameter scopes needs to be specified", ticket.Error.ErrorDescription);
        }

        [Fact]
        public async Task When_Resource_Does_Not_Exist_Then_Error_Is_Returned()
        {
            var ticket = await _umaClient.AddPermission(
                    new PostPermission { ResourceSetId = "resource", Scopes = new[] { "scope" } },
                    "header")
                .ConfigureAwait(false);

            Assert.True(ticket.ContainsError);
            Assert.Equal("invalid_resource_set_id", ticket.Error.Error);
            Assert.Equal("resource set resource doesn't exist", ticket.Error.ErrorDescription);
        }

        [Fact]
        public async Task When_Scopes_Does_Not_Exist_Then_Error_Is_Returned()
        {
            var resource = await _umaClient.AddResource(
                    new PostResourceSet { Name = "picture", Scopes = new [] { "read" } },
                    "header")
                .ConfigureAwait(false);

            var ticket = await _umaClient.AddPermission(
                    new PostPermission
                    {
                        ResourceSetId = resource.Content.Id,
                        Scopes = new[] { "scopescopescope" }
                    },
                    "header")
                .ConfigureAwait(false);

            Assert.True(ticket.ContainsError);
            Assert.Equal("invalid_scope", ticket.Error.Error);
            Assert.Equal("one or more scopes are not valid", ticket.Error.ErrorDescription);
        }

        [Fact]
        public async Task When_Adding_Permission_Then_TicketId_Is_Returned()
        {
            var resource = await _umaClient.AddResource(
                    new PostResourceSet { Name = "picture", Scopes = new [] { "read" } },
                    "header")
                .ConfigureAwait(false);

            var ticket = await _umaClient.AddPermission(
                    new PostPermission { ResourceSetId = resource.Content.Id, Scopes = new [] { "read" } },
                    "header")
                .ConfigureAwait(false);

            Assert.NotEmpty(ticket.Content.TicketId);
        }

        [Fact]
        public async Task When_Adding_Permissions_Then_TicketIds_Is_Returned()
        {
            var resource = await _umaClient.AddResource(
                    new PostResourceSet { Name = "picture", Scopes = new[] { "read" } },
                    "header")
                .ConfigureAwait(false);
            var permissions = new []
            {
                new PostPermission {ResourceSetId = resource.Content.Id, Scopes = new [] {"read"}},
                new PostPermission {ResourceSetId = resource.Content.Id, Scopes = new [] {"read"}}
            };

            var ticket = await _umaClient
                .AddPermissions("header", permissions)
                .ConfigureAwait(false);

            Assert.NotNull(ticket);
        }
    }
}
