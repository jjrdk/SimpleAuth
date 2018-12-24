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

namespace SimpleIdentityServer.Core.Api.Jws.Actions
{
    using System;
    using System.Threading.Tasks;
    using Errors;
    using Exceptions;
    using Extensions;
    using Helpers;
    using Jwt;
    using Jwt.Signature;
    using Parameters;
    using Results;
    using SimpleAuth.Shared;

    public class GetJwsInformationAction : IGetJwsInformationAction
    {
        private readonly IJwsParser _jwsParser;
        private readonly IJsonWebKeyHelper _jsonWebKeyHelper;
        private readonly IJsonWebKeyEnricher _jsonWebKeyEnricher;

        public GetJwsInformationAction(
            IJwsParser jwsParser,
            IJsonWebKeyHelper jsonWebKeyHelper,
            IJsonWebKeyEnricher jsonWebKeyEnricher)
        {
            _jwsParser = jwsParser;
            _jsonWebKeyHelper = jsonWebKeyHelper;
            _jsonWebKeyEnricher = jsonWebKeyEnricher;
        }
        
        public async Task<JwsInformationResult> Execute(GetJwsParameter getJwsParameter)
        {
            if (getJwsParameter == null || string.IsNullOrWhiteSpace(getJwsParameter.Jws))
            {
                throw new ArgumentNullException(nameof(getJwsParameter));
            }

            Uri uri = null;
            if (!string.IsNullOrWhiteSpace(getJwsParameter.Url))
            {
                if (!Uri.TryCreate(getJwsParameter.Url, UriKind.Absolute, out uri))
                {
                    throw new IdentityServerManagerException(
                        ErrorCodes.InvalidRequestCode,
                        string.Format(ErrorDescriptions.TheUrlIsNotWellFormed, getJwsParameter.Url));
                }
            }

            var jws = getJwsParameter.Jws;
            var jwsHeader = _jwsParser.GetHeader(jws);
            if (jwsHeader == null)
            {
                throw new IdentityServerManagerException(
                    ErrorCodes.InvalidRequestCode,
                    ErrorDescriptions.TheTokenIsNotAValidJws);
            }

            if (!string.Equals(jwsHeader.Alg, JwtConstants.JwsAlgNames.NONE, StringComparison.CurrentCultureIgnoreCase)
                && uri == null)
            {
                throw new IdentityServerManagerException(
                    ErrorCodes.InvalidRequestCode,
                    ErrorDescriptions.TheSignatureCannotBeChecked);
            }

            var result = new JwsInformationResult
            {
                Header = jwsHeader
            };

            JwsPayload payload;
            if (!string.Equals(jwsHeader.Alg, JwtConstants.JwsAlgNames.NONE, StringComparison.CurrentCultureIgnoreCase))
            {
                var jsonWebKey = await _jsonWebKeyHelper.GetJsonWebKey(jwsHeader.Kid, uri).ConfigureAwait(false);
                if (jsonWebKey == null)
                {
                    throw new IdentityServerManagerException(
                        ErrorCodes.InvalidRequestCode,
                        string.Format(ErrorDescriptions.TheJsonWebKeyCannotBeFound, jwsHeader.Kid, uri.AbsoluteUri));
                }

                payload = _jwsParser.ValidateSignature(jws, jsonWebKey);
                if (payload == null)
                {
                    throw new IdentityServerManagerException(
                        ErrorCodes.InvalidRequestCode,
                        ErrorDescriptions.TheSignatureIsNotCorrect);
                }

                var jsonWebKeyDic = _jsonWebKeyEnricher.GetJsonWebKeyInformation(jsonWebKey);
                jsonWebKeyDic.AddRange(_jsonWebKeyEnricher.GetPublicKeyInformation(jsonWebKey));
                result.JsonWebKey = jsonWebKeyDic;
            }
            else
            {
                payload = _jwsParser.GetPayload(jws);
            }


            result.Payload = payload;
            return result;
        }
    }
}
