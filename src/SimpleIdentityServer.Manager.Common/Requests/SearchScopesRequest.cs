﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SimpleIdentityServer.Manager.Common.Requests
{
    [DataContract]
    public class SearchScopesRequest
    {
        [JsonProperty(Constants.SearchScopeNames.ScopeTypes)]
        [DataMember(Name = Constants.SearchScopeNames.ScopeTypes)]
        public IList<int> ScopeTypes { get; set; }

        [JsonProperty(Constants.SearchScopeNames.ScopeNames)]
        [DataMember(Name = Constants.SearchScopeNames.ScopeNames)]
        public IEnumerable<string> ScopeNames { get; set; }

        [JsonProperty(Constants.SearchResponseNames.StartIndex)]
        [DataMember(Name = Constants.SearchResponseNames.StartIndex)]
        public int StartIndex { get; set; }

        [JsonProperty(Constants.SearchResponseNames.TotalResults)]
        [DataMember(Name = Constants.SearchResponseNames.TotalResults)]
        public int NbResults { get; set; }

        [JsonProperty(Constants.SearchScopeNames.Order)]
        [DataMember(Name = Constants.SearchScopeNames.Order)]
        public OrderRequest Order { get; set; }
    }
}
