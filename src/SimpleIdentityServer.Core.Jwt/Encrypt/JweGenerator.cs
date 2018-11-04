﻿// Copyright 2015 Habart Thierry
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

using SimpleIdentityServer.Core.Common;
using SimpleIdentityServer.Core.Common.Extensions;
using SimpleIdentityServer.Core.Jwt.Encrypt.Encryption;
using SimpleIdentityServer.Core.Jwt.Extensions;
using System;
using System.Linq;

namespace SimpleIdentityServer.Core.Jwt.Encrypt
{
    public class JweGenerator : IJweGenerator
    {
        private readonly IJweHelper _jweHelper;

        public JweGenerator(IJweHelper jweHelper)
        {
            _jweHelper = jweHelper;
        }

        public string GenerateJwe(
            string entry,
            JweAlg alg,
            JweEnc enc,
            JsonWebKey jsonWebKey)
        {
            return PerformeJweGeneration(entry, alg, enc, jsonWebKey, (encryption, jweProtectedHeader) => encryption.Encrypt(entry,
                alg,
                jweProtectedHeader,
                jsonWebKey)
            );
        }

        public string GenerateJweByUsingSymmetricPassword(
            string entry,
            JweAlg alg,
            JweEnc enc,
            JsonWebKey jsonWebKey,
            string password)
        {
            return PerformeJweGeneration(entry, alg, enc, jsonWebKey, (encryption, jweProtectedHeader) => encryption.EncryptWithSymmetricPassword(entry,
                alg,
                jweProtectedHeader,
                jsonWebKey,
                password)
            );
        }

        private string PerformeJweGeneration(
            string entry,
            JweAlg alg,
            JweEnc enc,
            JsonWebKey jsonWebKey,
            Func<IEncryption, JweProtectedHeader, AesEncryptionResult> callback)
        {
            var algo = JwtConstants.MappingNameToJweAlgEnum
                .SingleOrDefault(k => k.Value == alg);
            var encryption = JwtConstants.MappingNameToJweEncEnum
                .SingleOrDefault(k => k.Value == enc);
            if (jsonWebKey == null ||
                algo.IsDefault() ||
                encryption.IsDefault())
            {
                return entry;
            }

            // Construct the JWE protected header
            var jweProtectedHeader = new JweProtectedHeader
            {
                Alg = algo.Key,
                Enc = encryption.Key,
                Kid = jsonWebKey.Kid
            };

            var algorithm = _jweHelper.GetEncryptor(enc);
            var encryptionResult = callback(
                algorithm,
                jweProtectedHeader);

            var base64EncodedjweProtectedHeaderSerialized = jweProtectedHeader.SerializeWithDataContract().Base64Encode();
            var base64EncodedJweEncryptedKey = encryptionResult.EncryptedContentEncryptionKey.ToBase64Simplified();
            var base64EncodedIv = encryptionResult.Iv.ToBase64Simplified();
            var base64EncodedCipherText = encryptionResult.CipherText.ToBase64Simplified();
            var base64EncodedAuthenticationTag = encryptionResult.AuthenticationTag.ToBase64Simplified();

            return base64EncodedjweProtectedHeaderSerialized + "." +
                   base64EncodedJweEncryptedKey + "." +
                   base64EncodedIv + "." +
                   base64EncodedCipherText + "." +
                   base64EncodedAuthenticationTag;
        }
    }
}
