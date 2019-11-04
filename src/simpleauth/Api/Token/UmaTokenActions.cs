﻿namespace SimpleAuth.Api.Token
{
    using Authenticate;
    using JwtToken;
    using Parameters;
    using Policies;
    using Shared;
    using Shared.Events.Uma;
    using Shared.Models;
    using Shared.Responses;
    using SimpleAuth.Shared.Repositories;
    using SimpleAuth.Shared.Errors;
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class UmaTokenActions
    {
        private readonly ITicketStore _ticketStore;
        private readonly RuntimeSettings _configurationService;
        private readonly AuthorizationPolicyValidator _authorizationPolicyValidator;
        private readonly AuthenticateClient _authenticateClient;
        private readonly JwtGenerator _jwtGenerator;
        private readonly ITokenStore _tokenStore;
        private readonly IEventPublisher _eventPublisher;

        public UmaTokenActions(
            ITicketStore ticketStore,
            RuntimeSettings configurationService,
            IClientStore clientStore,
            IScopeRepository scopeRepository,
            ITokenStore tokenStore,
            IResourceSetRepository resourceSetRepository,
            IPolicyRepository policyRepository,
            IJwksStore jwksStore,
            IEventPublisher eventPublisher)
        {
            _ticketStore = ticketStore;
            _configurationService = configurationService;
            _authorizationPolicyValidator = new AuthorizationPolicyValidator(
                clientStore,
                jwksStore,
                policyRepository,
                resourceSetRepository,
                eventPublisher);
            _authenticateClient = new AuthenticateClient(clientStore, jwksStore);
            _jwtGenerator = new JwtGenerator(clientStore, scopeRepository, jwksStore);
            _tokenStore = tokenStore;
            _eventPublisher = eventPublisher;
        }

        public async Task<GenericResponse<GrantedToken>> GetTokenByTicketId(
            GetTokenViaTicketIdParameter parameter,
            AuthenticationHeaderValue authenticationHeaderValue,
            X509Certificate2 certificate,
            string issuerName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameter.Ticket))
            {
                return new GenericResponse<GrantedToken>
                {
                    HttpStatus = HttpStatusCode.BadRequest,
                    Error = new ErrorDetails
                    {
                        Status = HttpStatusCode.BadRequest,
                        Title = ErrorCodes.InvalidRequest,
                        Detail = string.Format(ErrorDescriptions.TheParameterNeedsToBeSpecified, UmaConstants.RptClaims.Ticket)
                    }
                };
            }

            var instruction = authenticationHeaderValue.GetAuthenticateInstruction(parameter, certificate);
            var authResult = await _authenticateClient.Authenticate(instruction, issuerName, cancellationToken)
                .ConfigureAwait(false);
            var client = authResult.Client;
            if (client == null)
            {
                return new GenericResponse<GrantedToken>
                {
                    HttpStatus = HttpStatusCode.BadRequest,
                    Error = new ErrorDetails
                    {
                        Status = HttpStatusCode.BadRequest,
                        Title = ErrorCodes.InvalidClient,
                        Detail = authResult.ErrorMessage
                    }
                };
            }

            if (client.GrantTypes == null || client.GrantTypes.All(x => x != GrantTypes.UmaTicket))
            {
                return new GenericResponse<GrantedToken>
                {
                    HttpStatus = HttpStatusCode.BadRequest,
                    Error = new ErrorDetails
                    {
                        Status = HttpStatusCode.BadRequest,
                        Title = ErrorCodes.InvalidGrant,
                        Detail = string.Format(
                            ErrorDescriptions.TheClientDoesntSupportTheGrantType,
                            client.ClientId,
                            GrantTypes.UmaTicket)
                    }
                };
            }

            var ticket = await _ticketStore.Get(parameter.Ticket, cancellationToken).ConfigureAwait(false);
            if (ticket == null)
            {
                return new GenericResponse<GrantedToken>
                {
                    HttpStatus = HttpStatusCode.BadRequest,
                    Error = new ErrorDetails
                    {
                        Status = HttpStatusCode.BadRequest,
                        Title = ErrorCodes.InvalidTicket,
                        Detail = string.Format(ErrorDescriptions.TheTicketDoesntExist, parameter.Ticket)
                    }
                };
            }

            // 4. Check the ticket.
            if (ticket.Expires < DateTimeOffset.UtcNow)
            {
                return new GenericResponse<GrantedToken>
                {
                    HttpStatus = HttpStatusCode.BadRequest,
                    Error = new ErrorDetails
                    {
                        Status = HttpStatusCode.BadRequest,
                        Title = ErrorCodes.ExpiredTicket,
                        Detail = ErrorDescriptions.TheTicketIsExpired
                    }
                };
            }

            var claimTokenParameter = new ClaimTokenParameter
            {
                Token = parameter.ClaimToken,
                Format = parameter.ClaimTokenFormat
            };

            // 4. Check the authorization.
            var authorizationResult = await _authorizationPolicyValidator
                .IsAuthorized(ticket, client.ClientId, claimTokenParameter, cancellationToken)
                .ConfigureAwait(false);

            if (authorizationResult.Type == AuthorizationPolicyResultEnum.Authorized
                || authorizationResult.Type == AuthorizationPolicyResultEnum.RequestSubmitted)
            {
                var grantedToken =
                    await GenerateToken(client, ticket.Lines, "openid", issuerName).ConfigureAwait(false);
                await _tokenStore.AddToken(grantedToken, cancellationToken).ConfigureAwait(false);
                await _ticketStore.Remove(ticket.Id, cancellationToken).ConfigureAwait(false);
                return new GenericResponse<GrantedToken>
                {
                    Content = grantedToken,
                    HttpStatus = HttpStatusCode.OK
                };
            }

            // 5. Generate a granted token.
            await _eventPublisher.Publish(
                    new UmaRequestNotAuthorized(Id.Create(), parameter.Ticket, parameter.ClientId, DateTimeOffset.UtcNow))
                .ConfigureAwait(false);
            return new GenericResponse<GrantedToken>
            {
                HttpStatus = HttpStatusCode.BadRequest,
                Error = new ErrorDetails
                {
                    Status = HttpStatusCode.BadRequest,
                    Title = ErrorCodes.RequestDenied,
                    Detail = ErrorDescriptions.TheAuthorizationPolicyIsNotSatisfied
                }
            };
        }

        private async Task<GrantedToken> GenerateToken(
            Client client,
            TicketLine[] ticketLines,
            string scope,
            string issuerName)
        {
            var expiresIn = _configurationService.RptLifeTime; // 1. Retrieve the expiration time of the granted token.
            var jwsPayload = await _jwtGenerator.GenerateAccessToken(client, scope.Split(' '), issuerName)
                .ConfigureAwait(false);

            // 2. Construct the JWT token (client).
            jwsPayload.Payload.Add(UmaConstants.RptClaims.Ticket, ticketLines);
            var handler = new JwtSecurityTokenHandler();
            var accessToken = handler.WriteToken(jwsPayload);

            return new GrantedToken
            {
                AccessToken = accessToken,
                RefreshToken = Id.Create(),
                ExpiresIn = (int)expiresIn.TotalSeconds,
                TokenType = CoreConstants.StandardTokenTypes.Bearer,
                CreateDateTime = DateTimeOffset.UtcNow,
                Scope = scope,
                ClientId = client.ClientId
            };
        }
    }
}
