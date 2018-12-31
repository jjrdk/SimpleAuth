﻿namespace SimpleAuth.Uma.Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Client.Configuration;
    using Client.Permission;
    using Client.Policy;
    using Client.ResourceSet;
    using Microsoft.Extensions.DependencyInjection;
    using Shared.DTOs;
    using Signature;
    using SimpleAuth;
    using SimpleAuth.Client;
    using SimpleAuth.Client.Operations;
    using SimpleAuth.Shared;
    using Xunit;

    public class TokenFixture : IClassFixture<TestUmaServerFixture>
    {
        private const string baseUrl = "http://localhost:5000";
        private IJwsGenerator _jwsGenerator;
        private ResourceSetClient _resourceSetClient;
        private PermissionClient _permissionClient;
        private PolicyClient _policyClient;
        private readonly TestUmaServerFixture _server;

        public TokenFixture(TestUmaServerFixture server)
        {
            _server = server;
        }

        [Fact]
        public async Task When_Ticket_Id_Does_Not_Exist_Then_Error_Is_Returned()
        {
            InitializeFakeObjects();

            var token = await new TokenClient(
                    TokenCredentials.FromClientCredentials("resource_server", "resource_server"),
                    TokenRequest.FromTicketId("ticket_id", ""),
                    _server.Client,
                    new GetDiscoveryOperation(_server
                        .Client)) // Try to get the access token via "ticket_id" grant-type.
                .ResolveAsync(baseUrl + "/.well-known/uma2-configuration")
                .ConfigureAwait(false);

            Assert.NotNull(token);
            Assert.True(token.ContainsError);
            Assert.Equal("invalid_ticket", token.Error.Error);
            Assert.Equal("the ticket ticket_id doesn't exist", token.Error.ErrorDescription);
        }

        [Fact]
        public async Task When_Using_ClientCredentials_Grant_Type_Then_AccessToken_Is_Returned()
        {
            InitializeFakeObjects();

            var result = await new TokenClient(
                    TokenCredentials.FromClientCredentials("resource_server", "resource_server"),
                    TokenRequest.FromScopes("uma_protection", "uma_authorization"),
                    _server.Client,
                    new GetDiscoveryOperation(_server.Client))
                .ResolveAsync(baseUrl + "/.well-known/uma2-configuration")
                .ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.NotEmpty(result.Content.AccessToken);
        }

        [Fact]
        public async Task When_Using_TicketId_Grant_Type_Then_AccessToken_Is_Returned()
        {
            InitializeFakeObjects();

            var jwsPayload = new JwsPayload
            {
                {"iss", "http://server.example.com"},
                {"sub", "248289761001"},
                {"aud", "s6BhdRkqt3"},
                {"nonce", "n-0S6_WzA2Mj"},
                {"exp", "1311281970"},
                {"iat", "1311280970"}
            };
            var jwt = _jwsGenerator.Generate(jwsPayload, JwsAlg.RS256, _server.SharedCtx.SignatureKey);

            var result = await new TokenClient(
                    TokenCredentials.FromClientCredentials("resource_server", "resource_server"), // Get PAT.
                    TokenRequest.FromScopes("uma_protection", "uma_authorization"),
                    _server.Client,
                    new GetDiscoveryOperation(_server.Client))
                .ResolveAsync(baseUrl + "/.well-known/uma2-configuration")
                .ConfigureAwait(false);
            var resource = await _resourceSetClient.AddByResolution(new PostResourceSet // Add ressource.
                    {
                        Name = "name",
                        Scopes = new List<string>
                        {
                            "read",
                            "write",
                            "execute"
                        }
                    },
                    baseUrl + "/.well-known/uma2-configuration",
                    result.Content.AccessToken)
                .ConfigureAwait(false);
            var addPolicy = await _policyClient.AddByResolution(new PostPolicy // Add an authorization policy.
                    {
                        Rules = new List<PostPolicyRule>
                        {
                            new PostPolicyRule
                            {
                                IsResourceOwnerConsentNeeded = false,
                                Scopes = new List<string>
                                {
                                    "read"
                                },
                                ClientIdsAllowed = new List<string>
                                {
                                    "resource_server"
                                },
                                Claims = new List<PostClaim>
                                {
                                    new PostClaim {Type = "sub", Value = "248289761001"}
                                }
                            }
                        },
                        ResourceSetIds = new List<string>
                        {
                            resource.Content.Id
                        }
                    },
                    baseUrl + "/.well-known/uma2-configuration",
                    result.Content.AccessToken)
                .ConfigureAwait(false);
            var ticket = await _permissionClient.AddByResolution(
                    new PostPermission // Add permission & retrieve a ticket id.
                    {
                        ResourceSetId = resource.Content.Id,
                        Scopes = new List<string>
                        {
                            "read"
                        }
                    },
                    baseUrl + "/.well-known/uma2-configuration",
                    "header")
                .ConfigureAwait(false);
            var token = await new TokenClient(
                    TokenCredentials.FromClientCredentials("resource_server",
                        "resource_server"), // Try to get the access token via "ticket_id" grant-type.
                    TokenRequest.FromTicketId(ticket.Content.TicketId, jwt),
                    _server.Client,
                    new GetDiscoveryOperation(_server.Client))
                .ResolveAsync(baseUrl + "/.well-known/uma2-configuration")
                .ConfigureAwait(false);

            Assert.NotNull(token);
        }

        private void InitializeFakeObjects()
        {
            var services = new ServiceCollection();
            services.AddSimpleAuthJwt();
            var provider = services.BuildServiceProvider();
            _jwsGenerator = provider.GetService<IJwsGenerator>();

            _resourceSetClient = new ResourceSetClient(_server.Client,
                new GetConfigurationOperation(_server.Client));
            _permissionClient = new PermissionClient(_server.Client,
                new GetConfigurationOperation(_server.Client));
            _policyClient = new PolicyClient(_server.Client,
                new GetConfigurationOperation(_server.Client));
        }
    }
}
