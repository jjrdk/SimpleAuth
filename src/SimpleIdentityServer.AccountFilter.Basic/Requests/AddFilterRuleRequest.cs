﻿namespace SimpleIdentityServer.AccountFilter.Basic.Requests
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [DataContract]
    public class AddFilterRuleRequest
    {
        [DataMember(Name = Constants.FilterRuleResponseNames.ClaimKey)]
        public string ClaimKey { get; set; }
        [DataMember(Name = Constants.FilterRuleResponseNames.ClaimValue)]
        public string ClaimValue { get; set; }
        [DataMember(Name = Constants.FilterRuleResponseNames.Operation)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ComparisonOperationsDto Operation { get; set; }
    }
}