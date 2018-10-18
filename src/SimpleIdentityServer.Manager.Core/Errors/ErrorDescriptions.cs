﻿#region copyright
// Copyright 2015 Habart Thierry
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

namespace SimpleIdentityServer.Manager.Core.Errors
{
    public static class ErrorDescriptions
    {
        public const string TheParameterIsMissing = "the parameter {0} is missing";
        public const string TheUrlIsNotWellFormed = "the url {0} is not well formed";
        public const string TheTokenIsNotAValidJws = "the token is not a valid JWS";
        public const string TheTokenIsNotAValidJwe = "the token is not a valid JWE";
        public const string TheJsonWebKeyCannotBeFound = "the json web key {0} cannot be found {1}";
        public const string TheSignatureIsNotCorrect = "the signature is not correct";
        public const string TheSignatureCannotBeChecked = "the signature cannot be checked if the URI is not specified";
        public const string TheJwsCannotBeGeneratedBecauseMissingParameters = "the jws cannot be generated because either the Url or Kid is not specified";
        public const string TheKtyIsNotSupported = "the kty '{0}' is not supported";
        public const string TheContentCannotBeExtractedFromJweToken = "the content cannot be extracted from the jwe token";
        public const string TheClientDoesntExist = "the client '{0}' doesn't exist";
        public const string MissingParameter = "the parameter {0} is missing";
        public const string TheScopeDoesntExist = "the scope '{0}' doesn't exist";
        public const string TheScopesDontExist = "the scopes '{0}' don't exist";
        public const string TheRedirectUriParameterIsNotValid = "one or more redirect_uri values are invalid";
        public const string TheRedirectUriContainsAFragment = "one or more redirect_uri contains a fragment";
        public const string ParameterIsNotCorrect = "the paramater {0} is not correct";
        public const string TheJwksParameterCannotBeSetBecauseJwksUrlIsUsed =
            "the jwks parameter cannot be set because the Jwks Url has already been set";
        public const string OneOrMoreSectorIdentifierUriIsNotARedirectUri =
            "one or more sector uri is not a redirect_uri";
        public const string TheParameterIsTokenEncryptedResponseAlgMustBeSpecified =
            "the parameter id_token_encrypted_response_alg must be specified";
        public const string OneOfTheRequestUriIsNotValid = "one of the request_uri is not valid";
        public const string TheParameterRequestObjectEncryptionAlgMustBeSpecified =
            "the parameter request_object_encryption_alg must be specified";                
        public static string TheParameterUserInfoEncryptedResponseAlgMustBeSpecified =
            "the parameter userinfo_encrypted_response_alg must be specified";
        public const string TheSectorIdentifierUrisCannotBeRetrieved = "the sector identifier uris cannot be retrieved";
        public const string TheResourceOwnerDoesntExist = "the resource owner {0} doesn't exist";
        public const string TheResourceOwnerMustBeConfirmed = "the account must be confirmed";
        public const string TheScopeAlreadyExists = "The scope {0} already exists";
        public const string TheFileExtensionIsNotCorrect = "the file extension is not correct";
        public const string TheFileIsNotWellFormed = "the file is not well formed";
        public const string ClaimExists = "a claim already exists with the same name";
        public const string ClaimDoesntExist = "the claim doesn't exist";
        public const string CannotInsertClaimIdentifier = "cannot insert claim identifier";
        public const string CannotRemoveClaimIdentifier = "cannot remove claim identifier";
        public const string ThePasswordCannotBeUpdated = "the password cannot be updated";
        public const string TheClaimsCannotBeUpdated = "the claims cannot be updated";
        public const string TheResourceOwnerCannotBeRemoved = "the resource owner cannot be removed";
        public const string TheClientCannotBeUpdated = "an error occured while trying to update the client";
        public const string TheClientCannotBeRemoved = "an error occured while trying to remove the client";
    }
}
