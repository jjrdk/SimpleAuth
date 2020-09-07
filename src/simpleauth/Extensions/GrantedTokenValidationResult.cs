﻿namespace SimpleAuth.Extensions
{
    internal class GrantedTokenValidationResult
    {
        public bool IsValid { get; set; }

        public string? MessageErrorCode { get; set; }

        public string? MessageErrorDescription { get; set; }
    }
}