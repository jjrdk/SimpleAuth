﻿namespace SimpleAuth.Client
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Shared;

    /// <summary>
    /// Defines the token request.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEnumerable{KeyValuePair}" />
    public class TokenRequest : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly Dictionary<string, string> _form;

        private TokenRequest(Dictionary<string, string> form)
        {
            _form = form;
        }

        /// <summary>
        /// Creates a request from the authorization code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="redirectUrl">The redirect URL.</param>
        /// <param name="codeVerifier">The code verifier.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// code
        /// or
        /// redirectUrl
        /// </exception>
        public static TokenRequest FromAuthorizationCode(string code, string redirectUrl, string codeVerifier = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException(nameof(code));
            }

            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                throw new ArgumentNullException(nameof(redirectUrl));
            }

            var dict = new Dictionary<string, string>
            {
                {RequestTokenNames.Code, code},
                {RequestTokenNames.GrantType, GrantTypes.AuthorizationCode},
                {RequestTokenNames.RedirectUri, redirectUrl}
            };
            if (!string.IsNullOrWhiteSpace(codeVerifier))
            {
                dict.Add(RequestTokenNames.CodeVerifier, codeVerifier);
            }

            return new TokenRequest(dict);
        }

        /// <summary>
        /// Creates a request the ticket identifier.
        /// </summary>
        /// <param name="ticketId">The ticket identifier.</param>
        /// <param name="claimToken">The claim token.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">ticketId</exception>
        public static TokenRequest FromTicketId(string ticketId, string claimToken)
        {
            if (string.IsNullOrWhiteSpace(ticketId))
            {
                throw new ArgumentNullException(nameof(ticketId));
            }


            var dict = new Dictionary<string, string>
            {
                {RequestTokenNames.GrantType, GrantTypes.UmaTicket},
                {"ticket", ticketId},
                {"claim_token_format", "http://openid.net/specs/openid-connect-core-1_0.html#IDToken"}
            };


            if (!string.IsNullOrWhiteSpace(claimToken))
            {
                dict.Add("claim_token", claimToken);
            }

            return new TokenRequest(dict);
        }

        /// <summary>
        /// Creates a request from the scopes.
        /// </summary>
        /// <param name="scopes">The scopes.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">scopes</exception>
        public static TokenRequest FromScopes(params string[] scopes)
        {
            if (scopes.Length == 0)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            var dict = new Dictionary<string, string>
            {
                {RequestTokenNames.Scope, string.Join(" ", scopes)},
                {RequestTokenNames.GrantType, GrantTypes.ClientCredentials}
            };

            return new TokenRequest(dict);
        }

        /// <summary>
        /// Creates a request from the refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">refreshToken</exception>
        public static TokenRequest FromRefreshToken(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentNullException(nameof(refreshToken));
            }

            var dict = new Dictionary<string, string>
            {
                {RequestTokenNames.GrantType, GrantTypes.RefreshToken},
                {RequestTokenNames.RefreshToken, refreshToken}
            };

            return new TokenRequest(dict);
        }

        /// <summary>
        /// Creates a request from the password.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="amrValues">The amr values.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// userName
        /// or
        /// password
        /// or
        /// scopes
        /// </exception>
        public static TokenRequest FromPassword(string userName, string password, string[] scopes, params string[] amrValues)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            if (scopes == null || scopes.Length == 0)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            var dict = new Dictionary<string, string>
            {
                { RequestTokenNames.Username, userName },
                { RequestTokenNames.Password, password },
                { RequestTokenNames.Scope, string.Join(" ", scopes) },
                { RequestTokenNames.GrantType, GrantTypes.Password }
            };
            if (amrValues.Length > 0)
            {
                dict.Add(RequestTokenNames.AmrValues, string.Join(" ", amrValues));
            }

            return new TokenRequest(dict);
        }

        /// <summary>
        /// Creates a request from the password.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="amrValues">The amr values.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// userName
        /// or
        /// password
        /// or
        /// scopes
        /// </exception>
        public static TokenRequest FromPassword(string userName, string password, string[] scopes, string amrValue = "pwd")
        {
            return FromPassword(userName, password, scopes, new[] {amrValue});
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _form.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}