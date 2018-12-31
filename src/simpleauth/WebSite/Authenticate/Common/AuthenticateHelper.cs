﻿namespace SimpleAuth.WebSite.Authenticate.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Api.Authorization;
    using Errors;
    using Exceptions;
    using Factories;
    using Helpers;
    using Parameters;
    using Results;
    using Shared.Models;
    using Shared.Repositories;
    using SimpleAuth.Common;

    public sealed class AuthenticateHelper : IAuthenticateHelper
    {
        private readonly IParameterParserHelper _parameterParserHelper;
        private readonly IActionResultFactory _actionResultFactory;
        private readonly IConsentHelper _consentHelper;
        private readonly IGenerateAuthorizationResponse _generateAuthorizationResponse;
        private readonly IClientStore _clientRepository;

        public AuthenticateHelper(IParameterParserHelper parameterParserHelper,
            IActionResultFactory actionResultFactory,
            IConsentHelper consentHelper,
            IGenerateAuthorizationResponse generateAuthorizationResponse,
            IClientStore clientRepository)
        {
            _parameterParserHelper = parameterParserHelper;
            _actionResultFactory = actionResultFactory;
            _consentHelper = consentHelper;
            _generateAuthorizationResponse = generateAuthorizationResponse;
            _clientRepository = clientRepository;
        }

        public async Task<EndpointResult> ProcessRedirection(
            AuthorizationParameter authorizationParameter,
            string code,
            string subject,
            List<Claim> claims,
            string issuerName)
        {
            if (authorizationParameter == null)
            {
                throw new ArgumentNullException(nameof(authorizationParameter));
            }

            var client = await _clientRepository.GetById(authorizationParameter.ClientId).ConfigureAwait(false);
            if (client == null)
            {
                throw new InvalidOperationException(string.Format(ErrorDescriptions.TheClientIdDoesntExist,
                    authorizationParameter.ClientId));
            }

            // Redirect to the consent page if the prompt parameter contains "consent"
            EndpointResult result;
            var prompts = _parameterParserHelper.ParsePrompts(authorizationParameter.Prompt);
            if (prompts != null &&
                prompts.Contains(PromptParameter.consent))
            {
                result = _actionResultFactory.CreateAnEmptyActionResultWithRedirection();
                result.RedirectInstruction.Action = SimpleAuthEndPoints.ConsentIndex;
                result.RedirectInstruction.AddParameter("code", code);
                return result;
            }

            var assignedConsent = await _consentHelper.GetConfirmedConsentsAsync(subject, authorizationParameter)
                .ConfigureAwait(false);

            // If there's already one consent then redirect to the callback
            if (assignedConsent != null)
            {
                result = _actionResultFactory.CreateAnEmptyActionResultWithRedirectionToCallBackUrl();
                var claimsIdentity = new ClaimsIdentity(claims, "simpleAuth");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                await _generateAuthorizationResponse
                    .ExecuteAsync(result, authorizationParameter, claimsPrincipal, client, issuerName)
                    .ConfigureAwait(false);
                var responseMode = authorizationParameter.ResponseMode;
                if (responseMode == ResponseMode.None)
                {
                    var responseTypes = _parameterParserHelper.ParseResponseTypes(authorizationParameter.ResponseType);
                    var authorizationFlow = GetAuthorizationFlow(responseTypes, authorizationParameter.State);
                    responseMode = GetResponseMode(authorizationFlow);
                }

                result.RedirectInstruction.ResponseMode = responseMode;
                return result;
            }

            // If there's no consent & there's no consent prompt then redirect to the consent screen.
            result = _actionResultFactory.CreateAnEmptyActionResultWithRedirection();
            result.RedirectInstruction.Action = SimpleAuthEndPoints.ConsentIndex;
            result.RedirectInstruction.AddParameter("code", code);
            return result;
        }

        private static AuthorizationFlow GetAuthorizationFlow(ICollection<ResponseType> responseTypes, string state)
        {
            if (responseTypes == null)
            {
                throw new SimpleAuthExceptionWithState(
                    ErrorCodes.InvalidRequestCode,
                    ErrorDescriptions.TheAuthorizationFlowIsNotSupported,
                    state);
            }

            var record = CoreConstants.MappingResponseTypesToAuthorizationFlows.Keys
                .SingleOrDefault(k => k.Count == responseTypes.Count && k.All(key => responseTypes.Contains(key)));
            if (record == null)
            {
                throw new SimpleAuthExceptionWithState(
                    ErrorCodes.InvalidRequestCode,
                    ErrorDescriptions.TheAuthorizationFlowIsNotSupported,
                    state);
            }

            return CoreConstants.MappingResponseTypesToAuthorizationFlows[record];
        }

        private static ResponseMode GetResponseMode(AuthorizationFlow authorizationFlow)
        {
            return CoreConstants.MappingAuthorizationFlowAndResponseModes[authorizationFlow];
        }
    }
}
