﻿#region copyright
// Copyright 2017 Habart Thierry
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
#endregion

using Newtonsoft.Json;
using SimpleBus.Core;
using SimpleIdentityServer.EventStore.Core.Models;
using SimpleIdentityServer.EventStore.Core.Repositories;
using SimpleIdentityServer.Handler.Events;
using System;
using System.Threading.Tasks;

namespace SimpleIdentityServer.EventStore.Handler.Handlers
{
    public class OpenIdErrorHandler : IHandle<OAuthErrorReceived>
    {
        private class Error
        {
            public string Code { get; set; }
            public string Message { get; set; }
            public string State { get; set; }
        }

        private readonly IEventAggregateRepository _repository;
        private readonly EventStoreHandlerOptions _options;

        public OpenIdErrorHandler(IEventAggregateRepository repository, EventStoreHandlerOptions options)
        {
            _repository = repository;
            _options = options;
        }

        public async Task Handle(OAuthErrorReceived evt)
        {
            if (evt == null)
            {
                throw new ArgumentNullException(nameof(evt));
            }

            var error = new Error
            {
                Code = evt.Code,
                Message = evt.Message,
                State = evt.State
            };
            var payload = JsonConvert.SerializeObject(error);
            await _repository.Add(new EventAggregate
            {
                Id = evt.Id,
                AggregateId = evt.ProcessId,
                CreatedOn = DateTime.UtcNow,
                Description = "An error occured",
                Payload =  payload,
                Order = evt.Order,
                Type = _options.Type,
                Verbosity = EventVerbosities.Error,
                Key = "error"
            });
        }
    }
}
