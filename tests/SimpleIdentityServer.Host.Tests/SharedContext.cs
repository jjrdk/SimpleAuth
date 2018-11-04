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

using Moq;
using SimpleIdentityServer.Core.Common;
using SimpleIdentityServer.Store;
using SimpleIdentityServer.Twilio.Client;
using System.Security.Cryptography;

namespace SimpleIdentityServer.Host.Tests
{
    using System.Net.Http;

    public class SharedContext
    {
        public SharedContext()
        {
            var serializedRsa = string.Empty;
            using (var provider = new RSACryptoServiceProvider())
            {
                serializedRsa = RsaExtensions.ToXmlString(provider, true);
            }

            SignatureKey = new JsonWebKey
            {
                Alg = AllAlg.RS256,
                KeyOps = new []
                {
                    KeyOperations.Sign,
                    KeyOperations.Verify
                },
                Kid = "1",
                Kty = KeyType.RSA,
                Use = Use.Sig,
                SerializedKey = serializedRsa,
            };
            ModelSignatureKey = new JsonWebKey
            {
                Alg = AllAlg.RS256,
                KeyOps = new []
                {
                    KeyOperations.Encrypt,
                    KeyOperations.Decrypt
                },
                Kid = "2",
                Kty = KeyType.RSA,
                Use = Use.Sig,
                SerializedKey = serializedRsa,
            };
            EncryptionKey = new JsonWebKey
            {
                Alg = AllAlg.RSA1_5,
                KeyOps = new[]
                {
                    KeyOperations.Decrypt,
                    KeyOperations.Encrypt
                },
                Kid = "3",
                Kty = KeyType.RSA,
                Use = Use.Enc,
                SerializedKey = serializedRsa,
            };
            ModelEncryptionKey = new JsonWebKey
            {
                Alg = AllAlg.RSA1_5,
                KeyOps = new[]
                {
                    KeyOperations.Encrypt,
                    KeyOperations.Decrypt
                },
                Kid = "4",
                Kty = KeyType.RSA,
                Use = Use.Enc,
                SerializedKey = serializedRsa,
            };
            ConfirmationCodeStore = new Mock<IConfirmationCodeStore>();
            TwilioClient = new Mock<ITwilioClient>();
        }

        public JsonWebKey EncryptionKey { get; }
        public JsonWebKey ModelEncryptionKey { get; }
        public JsonWebKey SignatureKey { get; }
        public JsonWebKey ModelSignatureKey { get; }
        public Mock<IConfirmationCodeStore> ConfirmationCodeStore { get; }
        public Mock<ITwilioClient> TwilioClient { get; }
        public HttpClient Client { get; set; }
    }
}
