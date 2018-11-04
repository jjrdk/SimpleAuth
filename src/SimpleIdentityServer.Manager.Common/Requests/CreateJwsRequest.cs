﻿#region copyright
// Copyright 2015 Habart Thierry
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

namespace SimpleIdentityServer.Manager.Common.Requests
{
    using Newtonsoft.Json;
    using System.Runtime.Serialization;
    using Shared;

    [DataContract]
    public class CreateJwsRequest
    {
        /// <summary>
        /// Gets or sets the JSON WEB KEY KID
        /// </summary>
        [JsonProperty(Constants.CreateJwsRequestNames.Kid)]
        [DataMember(Name = Constants.CreateJwsRequestNames.Kid)]
        public string Kid { get; set; }

        /// <summary>
        /// Gets or sets the sign alg
        /// </summary>
        [JsonProperty(Constants.CreateJwsRequestNames.Alg)]
        [DataMember(Name = Constants.CreateJwsRequestNames.Alg)]
        public JwsAlg Alg { get; set; }

        /// <summary>
        /// Gets or sets the JWKS URL
        /// </summary>
        [JsonProperty(Constants.CreateJwsRequestNames.Url)]
        [DataMember(Name = Constants.CreateJwsRequestNames.Url)]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the Payload
        /// </summary>
        [JsonProperty(Constants.CreateJwsRequestNames.Payload)]
        [DataMember(Name = Constants.CreateJwsRequestNames.Payload)]
        public JwsPayload Payload { get; set; }
    }
}
