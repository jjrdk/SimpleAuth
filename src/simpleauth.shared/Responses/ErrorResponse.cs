﻿namespace SimpleAuth.Shared.Responses
{
    using System.Runtime.Serialization;

    [DataContract]
    public class ErrorResponse
    {
        [DataMember(Name = "error")]
        public string Error { get; set; }
        [DataMember(Name = "error_description")]
        public string ErrorDescription { get; set; }
        [DataMember(Name = "error_uri")]
        public string ErrorUri { get; set; }
    }
}
