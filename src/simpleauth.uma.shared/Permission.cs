﻿namespace SimpleAuth.Uma.Shared
{
    using System.Collections.Generic;

    public class Permission
    {
        public string ResourceSetId { get; set; }
        public IEnumerable<string> Scopes { get; set; }
        public string Url { get; set; }
    }
}