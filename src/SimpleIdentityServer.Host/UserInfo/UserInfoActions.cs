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

namespace SimpleIdentityServer.Host.UserInfo
{
    using System;
    using System.Threading.Tasks;
    using Actions;
    using Microsoft.AspNetCore.Mvc;
    using SimpleAuth.Exceptions;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Events.Openid;

    public class UserInfoActions : IUserInfoActions
    {
        private readonly IGetJwsPayload _getJwsPayload;
        private readonly IEventPublisher _eventPublisher;

        public UserInfoActions(IGetJwsPayload getJwsPayload, IEventPublisher eventPublisher)
        {
            _getJwsPayload = getJwsPayload;
            _eventPublisher = eventPublisher;
        }

        public async Task<IActionResult> GetUserInformation(string accessToken)
        {
            var processId = Guid.NewGuid().ToString();
            try
            {
                _eventPublisher.Publish(new GetUserInformationReceived(Guid.NewGuid().ToString(), processId, accessToken, 0));
                var result = await _getJwsPayload.Execute(accessToken).ConfigureAwait(false);
                _eventPublisher.Publish(new UserInformationReturned(Guid.NewGuid().ToString(), processId, result, 1));
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
