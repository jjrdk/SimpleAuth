﻿namespace SimpleIdentityServer.Shared.Requests
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Shared;

    [DataContract]
    public class UpdateResourceOwnerClaimsRequest
    {
        [DataMember(Name = SharedConstants.ResourceOwnerResponseNames.Login)]
        public string Login { get; set; }
        [DataMember(Name = SharedConstants.ResourceOwnerResponseNames.Claims)]
        public List<KeyValuePair<string, string>> Claims { get; set; }
    }
}