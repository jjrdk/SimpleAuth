﻿namespace SimpleIdentityServer.Core.WebSite.Authenticate.Actions
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using Results;

    public class LocalOpenIdAuthenticationResult
    {
        public ActionResult ActionResult { get; set; }
        public ICollection<Claim> Claims { get; set; }
        public string TwoFactor { get; set; }
    }
}