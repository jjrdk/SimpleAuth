﻿// Copyright 2015 Habart Thierry
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SimpleIdentityServer.Manager.Common.Responses;
using SimpleIdentityServer.Manager.Core.Exceptions;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Manager.Host.Controllers
{
    using Core.Api.Manage;
    using SimpleIdentityServer.Core.Errors;
    using SimpleIdentityServer.Host.Extensions;

    [Route(Constants.EndPoints.Manage)]
    public class ManageController : Controller
    {
        private readonly IManageActions _manageActions;
        //private readonly IRepresentationManager _representationManager;

        public ManageController(IManageActions manageActions)
        {
            _manageActions = manageActions;
        }
        
        [HttpGet("export")]
        [Authorize("manager")]
        public async Task<IActionResult> Export()
        {
            var export = (await _manageActions.Export().ConfigureAwait(false)).ToDto();
            var json = JsonConvert.SerializeObject(export);
            return new FileContentResult(Encoding.UTF8.GetBytes(json), "application/json")
            {
                FileDownloadName = "export.json"
            };
        }

        [HttpPost("import")]
        [Authorize("manager")]
        public async Task<IActionResult> Import()
        {
            var files = Request.Form.Files;
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            var settingsFile = files.First();
            if (!settingsFile.FileName.EndsWith(".json"))
            {
                throw new IdentityServerManagerException(
                    ErrorCodes.UnhandledExceptionCode,
                    ErrorDescriptions.TheFileExtensionIsNotCorrect);
            }

            var content = string.Empty;
            using (var memoryStream = new MemoryStream())
            {
                await settingsFile.CopyToAsync(memoryStream).ConfigureAwait(false);
                memoryStream.Position = 0;
                using (var reader = new StreamReader(memoryStream))
                {
                    content = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            ExportResponse response = null;
            try
            {
                response = JsonConvert.DeserializeObject<ExportResponse>(content);
            }
            catch
            {
                throw new IdentityServerManagerException(ErrorCodes.UnhandledExceptionCode,
                    ErrorDescriptions.TheFileIsNotWellFormed);
            }

            if (!await _manageActions.Import(response.ToParameter()).ConfigureAwait(false))
            {
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }

            //await _representationManager.AddOrUpdateRepresentationAsync(this, ClientsController.GetClientsStoreName);
            return new NoContentResult();
        }
    }
}
