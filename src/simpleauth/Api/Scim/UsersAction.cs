﻿//// Copyright © 2015 Habart Thierry, © 2018 Jacob Reimers
//// 
//// Licensed under the Apache License, Version 2.0 (the "License");
//// you may not use this file except in compliance with the License.
//// You may obtain a copy of the License at
//// 
////     http://www.apache.org/licenses/LICENSE-2.0
//// 
//// Unless required by applicable law or agreed to in writing, software
//// distributed under the License is distributed on an "AS IS" BASIS,
//// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//// See the License for the specific language governing permissions and
//// limitations under the License.

//using Microsoft.AspNetCore.Http;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using SimpleIdentityServer.Scim.Core.Parsers;
//using SimpleIdentityServer.Scim.Core.Results;
//using System;
//using System.Threading.Tasks;

//namespace SimpleIdentityServer.Scim.Core.Apis
//{
//    using Common.Dtos.Events.Scim;
//    using SimpleIdentityServer.Core.Common;
//    using SimpleIdentityServer.Core.Common.DTOs;

//    internal class UsersAction : IUsersAction
//    {
//        private readonly ISearchParameterParser _searchParameterParser;
//        private readonly IEventPublisher _eventPublisher;

//        public UsersAction(
//            ISearchParameterParser searchParameterParser,
//            IEventPublisher eventPublisher)
//        {
//            _addRepresentationAction = addRepresentationAction;
//            _updateRepresentationAction = updateRepresentationAction;
//            _patchRepresentationAction = patchRepresentationAction;
//            _deleteRepresentationAction = deleteRepresentationAction;
//            _getRepresentationAction = getRepresentationAction;
//            _getRepresentationsAction = getRepresentationsAction;
//            _searchParameterParser = searchParameterParser;
//            _eventPublisher = eventPublisher;
//        }

//        public async Task<ApiActionResult> AddUser(ScimUser jObj, string locationPattern)
//        {
//            var processId = ClientId.Create();
//            try
//            {
//                _eventPublisher.Publish(new AddUserReceived(ClientId.Create();, processId, jObj.ToString(), 0));
//                var result = await _addRepresentationAction.Execute(jObj, locationPattern, ScimConstants.SchemaUrns.User, ScimConstants.ResourceTypes.User).ConfigureAwait(false);
//                _eventPublisher.Publish(new AddUserFinished(ClientId.Create();, processId, JsonConvert.SerializeObject(result).ToString(), 1));
//                return result;
//            }
//            catch (Exception ex)
//            {
//                _eventPublisher.Publish(new ScimErrorReceived(ClientId.Create();, processId, ex.Message, 1));
//                throw;
//            }
//        }

//        public async Task<ApiActionResult> UpdateUser(string id, JObject jObj, string locationPattern)
//        {
//            var processId = ClientId.Create();
//            try
//            {
//                _eventPublisher.Publish(new UpdateUserReceived(ClientId.Create();, processId, jObj.ToString(), 0));
//                var result = await _updateRepresentationAction.Execute(id, jObj, ScimConstants.SchemaUrns.User, locationPattern, ScimConstants.ResourceTypes.User).ConfigureAwait(false);
//                _eventPublisher.Publish(new UpdateUserFinished(ClientId.Create();, processId, JsonConvert.SerializeObject(result).ToString(), 1));
//                return result;
//            }
//            catch (Exception ex)
//            {
//                _eventPublisher.Publish(new ScimErrorReceived(ClientId.Create();, processId, ex.Message, 1));
//                throw;
//            }
//        }

//        public async Task<ApiActionResult> PatchUser(string id, JObject jObj, string locationPattern)
//        {
//            var processId = ClientId.Create();
//            try
//            {
//                _eventPublisher.Publish(new PatchUserReceived(ClientId.Create();, processId, jObj.ToString(), 0));
//                var result = await _patchRepresentationAction.Execute(id, jObj, ScimConstants.SchemaUrns.User, locationPattern).ConfigureAwait(false);
//                _eventPublisher.Publish(new PatchUserFinished(ClientId.Create();, processId, JsonConvert.SerializeObject(result).ToString(), 1));
//                return result;
//            }
//            catch (Exception ex)
//            {
//                _eventPublisher.Publish(new ScimErrorReceived(ClientId.Create();, processId, ex.Message, 1));
//                throw;
//            }
//        }

//        public async Task<ApiActionResult> RemoveUser(string id)
//        {
//            var processId = ClientId.Create();
//            try
//            {
//                var jObj = new JObject { { "id", id } };
//                _eventPublisher.Publish(new RemoveUserReceived(ClientId.Create();, processId, jObj.ToString(), 0));
//                var result = await _deleteRepresentationAction.Execute(id).ConfigureAwait(false);
//                _eventPublisher.Publish(new RemoveUserFinished(ClientId.Create();, processId, JsonConvert.SerializeObject(result).ToString(), 1));
//                return result;
//            }
//            catch (Exception ex)
//            {
//                _eventPublisher.Publish(new ScimErrorReceived(ClientId.Create();, processId, ex.Message, 1));
//                throw;
//            }
//        }

//        public Task<ApiActionResult> GetUser(string id, string locationPattern)
//        {
//            return _getRepresentationAction.Execute(id, locationPattern, ScimConstants.SchemaUrns.User);
//        }

//        public Task<ApiActionResult> SearchUsers(JObject jObj, string locationPattern)
//        {
//            var searchParam = _searchParameterParser.ParseJson(jObj);
//            return _getRepresentationsAction.Execute(ScimConstants.ResourceTypes.User, searchParam, locationPattern);
//        }

//        public Task<ApiActionResult> SearchUsers(IQueryCollection query, string locationPattern)
//        {
//            var searchParam = _searchParameterParser.ParseQuery(query);
//            return _getRepresentationsAction.Execute(ScimConstants.ResourceTypes.User, searchParam, locationPattern);
//        }
//    }
//}
