﻿namespace SimpleIdentityServer.Core.Jwt
{
    using System;

    public class ConfirmationCode
    {
        public string Value { get; set; }
        public string Subject { get; set; }
        public DateTime IssueAt { get; set; }
        public double ExpiresIn { get; set; }
    }
}
