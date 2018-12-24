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

using SimpleIdentityServer.Core.Api.UserInfo.Actions;
using SimpleIdentityServer.Core.Exceptions;
using System;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Core.Api.UserInfo
{
    using Microsoft.AspNetCore.Mvc;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Events.Openid;

    public class UserInfoActions : IUserInfoActions
    {
        private readonly IGetJwsPayload _getJwsPayload;
        private readonly IEventPublisher _eventPublisher;
        private readonly IPayloadSerializer _payloadSerializer;

        public UserInfoActions(IGetJwsPayload getJwsPayload, IEventPublisher eventPublisher,
            IPayloadSerializer payloadSerializer)
        {
            _getJwsPayload = getJwsPayload;
            _eventPublisher = eventPublisher;
            _payloadSerializer = payloadSerializer;
        }

        public async Task<IActionResult> GetUserInformation(string accessToken)
        {
            var processId = Guid.NewGuid().ToString();
            try
            {
                _eventPublisher.Publish(new GetUserInformationReceived(Guid.NewGuid().ToString(), processId, _payloadSerializer.GetPayload(accessToken), 0));
                var result = await _getJwsPayload.Execute(accessToken).ConfigureAwait(false);
                _eventPublisher.Publish(new UserInformationReturned(Guid.NewGuid().ToString(), processId, _payloadSerializer.GetPayload(result), 1));
                return result;
            }
            catch(IdentityServerException ex)
            {
                _eventPublisher.Publish(new OpenIdErrorReceived(Guid.NewGuid().ToString(), processId, ex.Code, ex.Message, 1));
                throw;
            }
        }
    }
}
