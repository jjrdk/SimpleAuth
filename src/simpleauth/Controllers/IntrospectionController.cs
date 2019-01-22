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

using SimpleAuth.Shared.Repositories;

namespace SimpleAuth.Controllers
{
    using Api.Introspection;
    using Errors;
    using Extensions;
    using Microsoft.AspNetCore.Mvc;
    using Shared.Requests;
    using Shared.Responses;
    using Shared.Serializers;
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    [Route(CoreConstants.EndPoints.Introspection)]
    public class IntrospectionController : Controller
    {
        private readonly PostIntrospectionAction _introspectionActions;

        public IntrospectionController(
            IClientStore clientStore,
            ITokenStore tokenStore)
        {
            _introspectionActions = new PostIntrospectionAction(clientStore, tokenStore);
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            try
            {

                if (Request.Form == null)
                {
                    return BuildError(ErrorCodes.InvalidRequestCode, "no parameter in body request", HttpStatusCode.BadRequest);
                }
            }
            catch (Exception)
            {
                return BuildError(ErrorCodes.InvalidRequestCode, "no parameter in body request", HttpStatusCode.BadRequest);
            }

            var nameValueCollection = new NameValueCollection();
            foreach (var kvp in Request.Form)
            {
                nameValueCollection.Add(kvp.Key, kvp.Value);
            }

            var serializer = new ParamSerializer();
            var introspectionRequest = serializer.Deserialize<IntrospectionRequest>(nameValueCollection);
            AuthenticationHeaderValue authenticationHeaderValue = null;
            if (Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            {
                var authorizationHeaderValue = authorizationHeader.First();
                var splittedAuthorizationHeaderValue = authorizationHeaderValue.Split(' ');
                if (splittedAuthorizationHeaderValue.Length == 2)
                {
                    authenticationHeaderValue = new AuthenticationHeaderValue(
                        splittedAuthorizationHeaderValue[0],
                        splittedAuthorizationHeaderValue[1]);
                }
            }

            var issuerName = Request.GetAbsoluteUriWithVirtualPath();
            var result = await _introspectionActions.Execute(introspectionRequest.ToParameter(), authenticationHeaderValue, issuerName).ConfigureAwait(false);
            return new OkObjectResult(result.ToDto());
        }

        /// <summary>
        /// Build the JSON error message.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        private static JsonResult BuildError(string code, string message, HttpStatusCode statusCode)
        {
            var error = new ErrorResponse
            {
                Error = code,
                ErrorDescription = message
            };
            return new JsonResult(error)
            {
                StatusCode = (int)statusCode
            };
        }
    }
}
