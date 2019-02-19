﻿// Copyright © 2015 Habart Thierry, © 2018 Jacob Reimers
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace SimpleAuth.Shared.Models
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using Microsoft.IdentityModel.Tokens;

    public class Client
    {
        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the client secrets.
        /// </summary>
        public ICollection<ClientSecret> Secrets { get; set; } = new List<ClientSecret>();

        public string ClientName { get; set; }

        public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the home page of the client.
        /// </summary>
        public Uri ClientUri { get; set; }

        /// <summary>
        /// Gets or sets the URL that the RP provides to the End-User to read about the how the profile data will be used.
        /// </summary>
        public Uri PolicyUri { get; set; }

        /// <summary>
        /// Gets or sets the URL that the RP provides to the End-User to read about the RP's terms of service.
        /// </summary>
        public Uri TosUri { get; set; }

        /// <summary>
        /// Gets or sets the JWS alg algorithm for signing the ID token issued to this client.
        /// The default is RS256. The public key for validating the signature is provided by retrieving the JWK Set referenced by the JWKS_URI
        /// </summary>
        public string IdTokenSignedResponseAlg { get; set; }

        /// <summary>
        /// Gets or sets the JWE alg algorithm. REQUIRED for encrypting the ID token issued to this client.
        /// The default is that no encryption is performed
        /// </summary>
        public string IdTokenEncryptedResponseAlg { get; set; }

        /// <summary>
        /// Gets or sets the JWE enc algorithm. REQUIRED for encrypting the ID token issued to this client.
        /// If IdTokenEncryptedResponseAlg is specified then the value is A128CBC-HS256
        /// </summary>
        public string IdTokenEncryptedResponseEnc { get; set; }

        /// <summary>
        /// Gets or sets the client authentication method for the Token Endpoint.
        /// </summary>
        public TokenEndPointAuthenticationMethods TokenEndPointAuthMethod { get; set; } =
            TokenEndPointAuthenticationMethods.ClientSecretBasic;

        /// <summary>
        /// Gets or sets an array containing a list of OAUTH2.0 response_type values
        /// </summary>
        public ICollection<string> ResponseTypes { get; set; } = ResponseTypeNames.All;

        /// <summary>
        /// Gets or sets an array containing a list of OAUTH2.0 grant types
        /// </summary>
        public ICollection<string> GrantTypes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a list of OAUTH2.0 grant_types.
        /// </summary>
        public ICollection<Scope> AllowedScopes { get; set; } = new List<Scope>();

        /// <summary>
        /// Gets or sets an array of Redirection URI values used by the client.
        /// </summary>
        public ICollection<Uri> RedirectionUrls { get; set; } = new List<Uri>();

        /// <summary>
        /// Gets or sets the type of application
        /// </summary>
        public ApplicationTypes ApplicationType { get; set; } = ApplicationTypes.Web;

        ///// <summary>
        ///// Url for the Client's JSON Web Key Set document
        ///// </summary>
        //public Uri JwksUri { get; set; }

        /// <summary>
        /// Gets or sets the list of json web keys
        /// </summary>
        public JsonWebKeySet JsonWebKeys { get; set; } = new JsonWebKeySet();

        /// <summary>
        /// Gets or sets the list of contacts
        /// </summary>
        public ICollection<string> Contacts { get; set; } = new List<string>();

        public ICollection<Claim> Claims { get; set; } = new List<Claim>();

        /// <summary>
        /// Get or set the sector identifier uri
        /// </summary>
        public Uri SectorIdentifierUri { get; set; }

        ///// <summary>
        ///// Gets or sets the subject type
        ///// </summary>
        //public string SubjectType { get; set; }

        /// <summary>
        /// Gets or sets the user info signed response algorithm
        /// </summary>
        public string UserInfoSignedResponseAlg { get; set; }

        /// <summary>
        /// Gets or sets the user info encrypted response algorithm
        /// </summary>
        public string UserInfoEncryptedResponseAlg { get; set; }

        /// <summary>
        /// Gets or sets the user info encrypted response enc
        /// </summary>
        public string UserInfoEncryptedResponseEnc { get; set; }

        /// <summary>
        /// Gets or sets the request objects signing algorithm
        /// </summary>
        public string RequestObjectSigningAlg { get; set; }

        /// <summary>
        /// Gets or sets the request object encryption algorithm
        /// </summary>
        public string RequestObjectEncryptionAlg { get; set; }

        /// <summary>
        /// Gets or sets the request object encryption enc
        /// </summary>
        public string RequestObjectEncryptionEnc { get; set; }

        /// <summary>
        /// Gets or sets the token endpoint authentication signing algorithm
        /// </summary>
        public string TokenEndPointAuthSigningAlg { get; set; }

        ///// <summary>
        ///// Gets or sets the default max age
        ///// </summary>
        //public double DefaultMaxAge { get; set; }

        /// <summary>
        /// Gets or sets the require authentication time
        /// </summary>
        public bool RequireAuthTime { get; set; }

        /// <summary>
        /// Gets or sets the default acr values
        /// </summary>
        public string DefaultAcrValues { get; set; }

        /// <summary>
        /// Gets or sets the initiate login uri
        /// </summary>
        public Uri InitiateLoginUri { get; set; }

        /// <summary>
        /// Gets or sets the list of request uris
        /// </summary>
        public ICollection<Uri> RequestUris { get; set; } = new List<Uri>();

        /// <summary>
        /// Gets or sets use SCIM protocol to access user information.
        /// </summary>
        public bool ScimProfile { get; set; }

        /// <summary>
        /// Client require PKCE.
        /// </summary>
        public bool RequirePkce { get; set; }

        /// <summary>
        /// Get or sets the post logout redirect uris.
        /// </summary>
        public ICollection<Uri> PostLogoutRedirectUris { get; set; } = new List<Uri>();
    }
}
