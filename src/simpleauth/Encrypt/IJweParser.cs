﻿namespace SimpleAuth.Encrypt
{
    using Shared;

    public interface IJweParser
    {
        string Parse(
            string jwe,
            JsonWebKey jsonWebKey);

        string ParseByUsingSymmetricPassword(
            string jwe,
            JsonWebKey jsonWebKey,
            string password);

        JweProtectedHeader GetHeader(string jwe);
    }
}