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

namespace SimpleAuth.Shared.Requests
{
    using System.Runtime.Serialization;

    [DataContract]
    public class CreateJweRequest
    {
        [DataMember(Name = SharedConstants.CreateJweRequestNames.Jws)]
        public string Jws { get; set; }

        [DataMember(Name = SharedConstants.CreateJweRequestNames.Url)]
        public string Url { get; set; }

        [DataMember(Name = SharedConstants.CreateJweRequestNames.Kid)]
        public string Kid { get; set; }

        [DataMember(Name = SharedConstants.CreateJweRequestNames.Alg)]
        public string Alg { get; set; }

        [DataMember(Name = SharedConstants.CreateJweRequestNames.Enc)]
        public string Enc { get; set; }

        [DataMember(Name = SharedConstants.CreateJweRequestNames.Password)]
        public string Password { get; set; }
    }
}
