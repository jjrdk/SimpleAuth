﻿// Copyright © 2015 Habart Thierry, © 2018 Jacob Reimers
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

namespace SimpleAuth.Policies
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Errors;
    using Exceptions;
    using Parameters;
    using Repositories;
    using Shared;
    using Shared.Events.Uma;
    using Shared.Models;
    using Shared.Responses;

    internal class AuthorizationPolicyValidator : IAuthorizationPolicyValidator
    {
        private readonly IBasicAuthorizationPolicy _basicAuthorizationPolicy;
        private readonly IResourceSetRepository _resourceSetRepository;
        private readonly IEventPublisher _eventPublisher;

        public AuthorizationPolicyValidator(
            IBasicAuthorizationPolicy basicAuthorizationPolicy,
            IResourceSetRepository resourceSetRepository,
            IEventPublisher eventPublisher)
        {
            _basicAuthorizationPolicy = basicAuthorizationPolicy;
            _resourceSetRepository = resourceSetRepository;
            _eventPublisher = eventPublisher;
        }

        public async Task<AuthorizationPolicyResult> IsAuthorized(Ticket validTicket, string clientId, ClaimTokenParameter claimTokenParameter)
        {
            if (validTicket == null)
            {
                throw new ArgumentNullException(nameof(validTicket));
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            if (validTicket.Lines == null || !validTicket.Lines.Any())
            {
                throw new ArgumentNullException(nameof(validTicket.Lines));
            }

            var resourceIds = validTicket.Lines.Select(l => l.ResourceSetId);
            var resources = await _resourceSetRepository.Get(resourceIds).ConfigureAwait(false);
            if (resources == null || !resources.Any() || resources.Count() != resourceIds.Count())
            {
                throw new SimpleAuthException(ErrorCodes.InternalError, ErrorDescriptions.SomeResourcesDontExist);
            }

            AuthorizationPolicyResult validationResult = null;
            foreach (var ticketLine in validTicket.Lines)
            {
                var ticketLineParameter = new TicketLineParameter(clientId, ticketLine.Scopes, validTicket.IsAuthorizedByRo);
                var resource = resources.First(r => r.Id == ticketLine.ResourceSetId);
                validationResult = await Validate(ticketLineParameter, resource, claimTokenParameter).ConfigureAwait(false);
                if (validationResult.Type != AuthorizationPolicyResultEnum.Authorized)
                {
                    await _eventPublisher.Publish(new AuthorizationPolicyNotAuthorized(
                            Id.Create(),
                            validTicket.Id,
                            DateTime.UtcNow))
                        .ConfigureAwait(false);

                    return validationResult;
                }
            }

            return validationResult;
        }

        private async Task<AuthorizationPolicyResult> Validate(TicketLineParameter ticketLineParameter, ResourceSet resource, ClaimTokenParameter claimTokenParameter)
        {
            if (resource.Policies == null || !resource.Policies.Any())
            {
                return new AuthorizationPolicyResult
                {
                    Type = AuthorizationPolicyResultEnum.Authorized
                };
            }

            foreach (var authorizationPolicy in resource.Policies)
            {
                var result = await _basicAuthorizationPolicy.Execute(ticketLineParameter, authorizationPolicy, claimTokenParameter).ConfigureAwait(false);
                if (result.Type == AuthorizationPolicyResultEnum.Authorized)
                {
                    return result;
                }
            }

            return new AuthorizationPolicyResult
            {
                Type = AuthorizationPolicyResultEnum.NotAuthorized
            };
        }
    }
}
