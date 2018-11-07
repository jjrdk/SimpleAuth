﻿using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SimpleIdentityServer.Manager.Common.Requests
{
    using Shared;

    [DataContract]
    public class UpdateResourceOwnerClaimsRequest
    {
        [DataMember(Name = Constants.ResourceOwnerResponseNames.Login)]
        public string Login { get; set; }
        [DataMember(Name = Constants.ResourceOwnerResponseNames.Claims)]
        public List<KeyValuePair<string, string>> Claims { get; set; }
    }
}