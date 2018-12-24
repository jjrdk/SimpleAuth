﻿// Copyright 2016 Habart Thierry
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

namespace SimpleAuth.Shared
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class SearchParameter
    {
        [DataMember(Name = ScimConstants.SearchParameterNames.Attributes)]
        public IEnumerable<string> Attributes { get; set; }

        [DataMember(Name = ScimConstants.SearchParameterNames.ExcludedAttributes)]
        public IEnumerable<string> ExcludedAttributes { get; set; }

        [DataMember(Name = ScimConstants.SearchParameterNames.Filter)]
        public string Filter { get; set; }

        [DataMember(Name = ScimConstants.SearchParameterNames.SortBy)]
        public string SortBy { get; set; }

        [DataMember(Name = ScimConstants.SearchParameterNames.SortOrder)]
        public SortOrders SortOrder { get; set; } = SortOrders.Ascending;

        [DataMember(Name = ScimConstants.SearchParameterNames.StartIndex)]
        public int StartIndex { get; set; }

        [DataMember(Name = ScimConstants.SearchParameterNames.Count)]
        public int Count { get; set; } = int.MaxValue;
    }
}
