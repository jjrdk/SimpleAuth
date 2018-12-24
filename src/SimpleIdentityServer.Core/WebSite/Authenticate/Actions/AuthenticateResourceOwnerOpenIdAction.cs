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

using System;
using System.Linq;
using System.Security.Claims;

using SimpleIdentityServer.Core.Extensions;
using SimpleIdentityServer.Core.Factories;
using SimpleIdentityServer.Core.Helpers;
using SimpleIdentityServer.Core.Parameters;
using SimpleIdentityServer.Core.Results;
using SimpleIdentityServer.Core.WebSite.Authenticate.Common;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Core.WebSite.Authenticate.Actions
{
    public class AuthenticateResourceOwnerOpenIdAction : IAuthenticateResourceOwnerOpenIdAction
    {
        private readonly IParameterParserHelper _parameterParserHelper;
        private readonly IActionResultFactory _actionResultFactory;
        private readonly IAuthenticateHelper _authenticateHelper;

        public AuthenticateResourceOwnerOpenIdAction(
            IParameterParserHelper parameterParserHelper,
            IActionResultFactory actionResultFactory,
            IAuthenticateHelper authenticateHelper)
        {
            _parameterParserHelper = parameterParserHelper;
            _actionResultFactory = actionResultFactory;
            _authenticateHelper = authenticateHelper;
        }


        /// <summary>
        /// Returns an action result to the controller's action.
        /// 1). Redirect to the consent screen if the user is authenticated AND the request doesn't contain a login prompt.
        /// 2). Do nothing
        /// </summary>
        /// <param name="authorizationParameter">The parameter</param>
        /// <param name="resourceOwnerPrincipal">Resource owner principal</param>
        /// <param name="code">Encrypted parameter</param>
        /// <returns>Action result to the controller's action</returns>
        public async Task<EndpointResult> Execute(
            AuthorizationParameter authorizationParameter,
            ClaimsPrincipal resourceOwnerPrincipal,
            string code, string issuerName)
        {
            if (authorizationParameter == null)
            {
                throw new ArgumentNullException(nameof(authorizationParameter));    
            }

            var resourceOwnerIsAuthenticated = resourceOwnerPrincipal.IsAuthenticated();
            var promptParameters = _parameterParserHelper.ParsePrompts(authorizationParameter.Prompt);

            // 1).
            if (resourceOwnerIsAuthenticated && 
                promptParameters != null && 
                !promptParameters.Contains(PromptParameter.login))
            {
                var subject = resourceOwnerPrincipal.GetSubject();
                var claims = resourceOwnerPrincipal.Claims.ToList();
                return await _authenticateHelper.ProcessRedirection(authorizationParameter,
                    code,
                    subject,
                    claims, issuerName).ConfigureAwait(false);
            }

            // 2).
            return _actionResultFactory.CreateAnEmptyActionResultWithNoEffect();
        }
    }
}
