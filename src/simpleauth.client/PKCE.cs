﻿namespace SimpleAuth.Client
{
    /// <summary>
    /// Defines the PKCE
    /// </summary>
    public class Pkce
    {
        /// <summary>
        /// Gets or sets the code verifier.
        /// </summary>
        /// <value>
        /// The code verifier.
        /// </value>
        public string CodeVerifier { get; set; }

        /// <summary>
        /// Gets or sets the code challenge.
        /// </summary>
        /// <value>
        /// The code challenge.
        /// </value>
        public string CodeChallenge { get; set; }
    }
}