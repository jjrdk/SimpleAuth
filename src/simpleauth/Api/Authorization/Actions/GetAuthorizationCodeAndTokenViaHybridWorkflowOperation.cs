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

namespace SimpleAuth.Api.Authorization.Actions
{
    using System;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Common;
    using Errors;
    using Exceptions;
    using Logging;
    using Parameters;
    using Results;
    using Shared.Models;
    using SimpleAuth.Common;
    using Validators;

    public sealed class GetAuthorizationCodeAndTokenViaHybridWorkflowOperation : IGetAuthorizationCodeAndTokenViaHybridWorkflowOperation
    {
        private readonly IOAuthEventSource _oauthEventSource;
        private readonly IProcessAuthorizationRequest _processAuthorizationRequest;
        private readonly IClientValidator _clientValidator;
        private readonly IGenerateAuthorizationResponse _generateAuthorizationResponse;

        public GetAuthorizationCodeAndTokenViaHybridWorkflowOperation(
            IOAuthEventSource oauthEventSource,
            IProcessAuthorizationRequest processAuthorizationRequest,
            IClientValidator clientValidator,
            IGenerateAuthorizationResponse generateAuthorizationResponse)
        {
            _oauthEventSource = oauthEventSource;
            _processAuthorizationRequest = processAuthorizationRequest;
            _clientValidator = clientValidator;
            _generateAuthorizationResponse = generateAuthorizationResponse;
        }

        public async Task<EndpointResult> Execute(AuthorizationParameter authorizationParameter, IPrincipal principal, Client client, string issuerName)
        {
            if (authorizationParameter == null)
            {
                throw new ArgumentNullException(nameof(authorizationParameter));
            }

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (string.IsNullOrWhiteSpace(authorizationParameter.Nonce))
            {
                throw new SimpleAuthExceptionWithState(
                    ErrorCodes.InvalidRequestCode,
                    string.Format(ErrorDescriptions.MissingParameter, CoreConstants.StandardAuthorizationRequestParameterNames.NonceName),
                    authorizationParameter.State);
            }

            var claimsPrincipal = principal as ClaimsPrincipal;

            _oauthEventSource.StartHybridFlow(
                authorizationParameter.ClientId,
                authorizationParameter.Scope,
                authorizationParameter.Claims == null ? string.Empty : authorizationParameter.Claims.ToString());
            var result = await _processAuthorizationRequest.ProcessAsync(authorizationParameter, claimsPrincipal, client, issuerName).ConfigureAwait(false);
            if (!_clientValidator.CheckGrantTypes(client, GrantType.@implicit, GrantType.authorization_code))
            {
                throw new SimpleAuthExceptionWithState(
                    ErrorCodes.InvalidRequestCode,
                    string.Format(ErrorDescriptions.TheClientDoesntSupportTheGrantType,
                        authorizationParameter.ClientId,
                        "implicit"),
                    authorizationParameter.State);
            }

            if (result.Type == TypeActionResult.RedirectToCallBackUrl)
            {
                if (claimsPrincipal == null)
                {
                    throw new SimpleAuthExceptionWithState(
                        ErrorCodes.InvalidRequestCode,
                        ErrorDescriptions.TheResponseCannotBeGeneratedBecauseResourceOwnerNeedsToBeAuthenticated,
                        authorizationParameter.State);
                }

                await _generateAuthorizationResponse.ExecuteAsync(result, authorizationParameter, claimsPrincipal, client, issuerName).ConfigureAwait(false);
            }

            var actionTypeName = Enum.GetName(typeof(TypeActionResult), result.Type);
            _oauthEventSource.EndHybridFlow(
                authorizationParameter.ClientId,
                actionTypeName,
                result.RedirectInstruction == null ? string.Empty : Enum.GetName(typeof(SimpleAuthEndPoints), result.RedirectInstruction.Action));

            return result;
        }
    }
}
