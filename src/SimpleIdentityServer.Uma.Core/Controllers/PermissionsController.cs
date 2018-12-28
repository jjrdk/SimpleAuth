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

namespace SimpleAuth.Uma.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Api.PermissionController;
    using Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Shared.Responses;
    using SimpleIdentityServer.Uma.Common.DTOs;
    using ErrorCodes = SimpleAuth.Errors.ErrorCodes;

    [Route(UmaConstants.RouteValues.Permission)]
    public class PermissionsController : Controller
    {
        private readonly IPermissionControllerActions _permissionControllerActions;

        public PermissionsController(IPermissionControllerActions permissionControllerActions)
        {
            _permissionControllerActions = permissionControllerActions;
        }

        [HttpPost]
        [Authorize("UmaProtection")]
        public async Task<IActionResult> PostPermission([FromBody] PostPermission postPermission)
        {
            if (postPermission == null)
            {
                return BuildError(ErrorCodes.InvalidRequestCode, "no parameter in body request", HttpStatusCode.BadRequest);
            }

            var parameter = postPermission.ToParameter();
            var clientId = this.GetClientId();
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return BuildError(ErrorCodes.InvalidRequestCode, "the client_id cannot be extracted", HttpStatusCode.BadRequest);
            }

            var ticketId = await _permissionControllerActions.Add(parameter, clientId).ConfigureAwait(false);
            var result = new AddPermissionResponse
            {
                TicketId = ticketId
            };
            return new ObjectResult(result)
            {
                StatusCode = (int)HttpStatusCode.Created
            };
        }

        [HttpPost("bulk")]
        [Authorize("UmaProtection")]
        public async Task<IActionResult> PostPermissions([FromBody] IEnumerable<PostPermission> postPermissions)
        {
            if (postPermissions == null)
            {
                return BuildError(ErrorCodes.InvalidRequestCode, "no parameter in body request", HttpStatusCode.BadRequest);
            }

            var parameters = postPermissions.Select(p => p.ToParameter());
            var clientId = this.GetClientId();
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return BuildError(ErrorCodes.InvalidRequestCode, "the client_id cannot be extracted", HttpStatusCode.BadRequest);
            }

            var ticketId = await _permissionControllerActions.Add(parameters, clientId).ConfigureAwait(false);
            var result = new AddPermissionResponse
            {
                TicketId = ticketId
            };
            return new ObjectResult(result)
            {
                StatusCode = (int)HttpStatusCode.Created
            };
        }

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
