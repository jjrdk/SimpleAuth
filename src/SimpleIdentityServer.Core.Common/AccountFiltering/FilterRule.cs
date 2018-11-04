﻿namespace SimpleIdentityServer.Core.Common.AccountFiltering
{
    public sealed class FilterRule
    {
        public string ClaimKey { get; set; }
        public string ClaimValue { get; set; }
        public ComparisonOperations Operation { get; set; }
    }
}
