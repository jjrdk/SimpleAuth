﻿#region copyright
// Copyright © 2015 Habart Thierry, © 2018 Jacob Reimers
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
#endregion

namespace SimpleIdentityServer.Manager.Client
{
    internal static class ManagerClientConstants
    {
        public static class ConfigurationResponseNames
        {
            public const string JwsEndpoint = "jws_endpoint";
            public const string JweEndpoint = "jwe_endpoint";
            public const string ClientsEndpoint = "clients_endpoint";
            public const string ScopesEndpoint = "scopes_endpoint";
            public const string ResourceOwnersEndpoint = "resourceowners_endpoint";
            public const string ManageEndpoint = "manage_endpoint";
            public const string ClaimsEndpoint = "claims_endpoint";
        }

        public static class GetClientsResponseNames
        {
            public const string ClientId = "client_id";
            public const string Secrets = "secrets";
            public const string RedirectUris = "redirect_uris";
            public const string ResponseTypes = "response_types";
            public const string GrantTypes = "grant_types";
            public const string ApplicationType = "application_type";
            public const string Contacts = "contacts";
            public const string ClientName = "client_name";
            public const string LogoUri = "logo_uri";
            public const string ClientUri = "client_uri";
            public const string PolicyUri = "policy_uri";
            public const string TosUri = "tos_uri";
            public const string JwksUri = "jwks_uri";
            public const string Jwks = "jwks";
            public const string SectoreIdentifierUri = "sector_identifier_uri";
            public const string SubjectType = "subject_type";
            public const string IdTokenSignedResponseAlg = "id_token_signed_response_alg";
            public const string IdTokenEncryptedResponseAlg = "id_token_encrypted_response_alg";
            public const string IdTokenEncryptedResponseEnc = "id_token_encrypted_response_enc";
            public const string UserInfoSignedResponseAlg = "userinfo_signed_response_alg";
            public const string UserInfoEncryptedResponseAlg = "userinfo_encrypted_response_alg";
            public const string UserInfoEncryptedResponseEnc = "userinfo_encrypted_response_enc";
            public const string RequestObjectSigningAlg = "request_object_signing_alg";
            public const string RequestObjectEncryptionAlg = "request_object_encryption_alg";
            public const string RequestObjectEncryptionEnc = "request_object_encryption_enc";
            public const string TokenEndPointAuthMethod = "token_endpoint_auth_method";
            public const string TokenEndPointAuthSigningAlg = "token_endpoint_auth_signing_alg";
            public const string DefaultMaxAge = "default_max_age";
            public const string RequireAuthTime = "require_auth_time";
            public const string DefaultAcrValues = "default_acr_values";
            public const string InitiateLoginUri = "initiate_login_uri";
            public const string RequestUris = "request_uris";
            public const string AllowedScopes = "allowed_scopes";
            public const string JsonWebKeys = "json_web_keys";
            public const string RedirectionUrls = "redirection_urls";
            public const string SectorIdentifierUri = "sector_identifier_uri";
        }

        public static class SearchClientResponseNames
        {
            public const string Content = "content";
            public const string StartIndex = "start_index";
            public const string Count = "count";
        }
    }
}
