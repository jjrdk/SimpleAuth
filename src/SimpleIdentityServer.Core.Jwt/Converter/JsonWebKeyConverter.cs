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

using SimpleIdentityServer.Core.Jwt.Exceptions;
using SimpleIdentityServer.Core.Jwt.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace SimpleIdentityServer.Core.Jwt.Converter
{
    using Extensions;
    using Shared;
    using Shared.Requests;

    public class JsonWebKeyConverter : IJsonWebKeyConverter
    {
        public IEnumerable<JsonWebKey> ExtractSerializedKeys(JsonWebKeySet jsonWebKeySet)
        {
            if (jsonWebKeySet == null)
            {
                throw new ArgumentNullException(nameof(jsonWebKeySet));
            }

            if (jsonWebKeySet.Keys == null ||
                    !jsonWebKeySet.Keys.Any())
            {
                return new List<JsonWebKey>();
            }

            var result = new List<JsonWebKey>();
            foreach (var jsonWebKey in jsonWebKeySet.Keys)
            {
                var keyType = jsonWebKey.FirstOrDefault(j => j.Key == JwtConstants.JsonWebKeyParameterNames.KeyTypeName);
                var use = jsonWebKey.FirstOrDefault(j => j.Key == JwtConstants.JsonWebKeyParameterNames.UseName);
                var kid =
                    jsonWebKey.FirstOrDefault(j => j.Key == JwtConstants.JsonWebKeyParameterNames.KeyIdentifierName);
                if (keyType.Equals(default(KeyValuePair<string, object>)) ||
                    use.Equals(default(KeyValuePair<string, object>)) ||
                    kid.Equals(default(KeyValuePair<string, object>)) ||
                    !JwtConstants.MappingKeyTypeEnumToName.Values.Contains(keyType.Value) ||
                    !JwtConstants.MappingUseEnumerationToName.Values.Contains(use.Value))
                {
                    throw new InvalidOperationException(ErrorDescriptions.JwkIsInvalid);
                }

                var useEnum = JwtConstants.MappingUseEnumerationToName
                    .FirstOrDefault(m => m.Value == use.Value.ToString()).Key;
                var keyTypeEnum = JwtConstants.MappingKeyTypeEnumToName
                    .FirstOrDefault(k => k.Value == keyType.Value.ToString()).Key;

                var jsonWebKeyInformation = new JsonWebKey
                {
                    Use = useEnum,
                    Kid = kid.Value.ToString(),
                    Kty = keyTypeEnum
                };
                jsonWebKeyInformation.Use = useEnum;


                var serializedKey = string.Empty;
                switch (keyType.Value.ToString())
                {
                    case JwtConstants.KeyTypeValues.RsaName:
                        serializedKey = ExtractRsaKeyInformation(jsonWebKey);
                        break;
                    case JwtConstants.KeyTypeValues.EcName:
                        serializedKey = ExtractEcKeyInformation(jsonWebKey);
                        break;
                }

                jsonWebKeyInformation.SerializedKey = serializedKey;
                result.Add(jsonWebKeyInformation);
            }

            return result;
        }

        private static string ExtractRsaKeyInformation(Dictionary<string, object> information)
        {
            var modulusKeyPair = information.FirstOrDefault(i => i.Key == JwtConstants.JsonWebKeyParameterNames.RsaKey.ModulusName);
            var exponentKeyPair = information.FirstOrDefault(i => i.Key == JwtConstants.JsonWebKeyParameterNames.RsaKey.ExponentName);
            if (modulusKeyPair.Equals(default(KeyValuePair<string, object>)) ||
                exponentKeyPair.Equals(default(KeyValuePair<string, object>)))
            {
                throw new InvalidOperationException(ErrorDescriptions.CannotExtractParametersFromJsonWebKey);
            }

            var rsaParameters = new RSAParameters
            {
                Modulus = modulusKeyPair.Value.ToString().Base64DecodeBytes(),
                Exponent = exponentKeyPair.Value.ToString().Base64DecodeBytes()
            };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var rsaCryptoServiceProvider = new RSACryptoServiceProvider())
                {
                    rsaCryptoServiceProvider.ImportParameters(rsaParameters);
                    return RsaExtensions.ToXmlString(rsaCryptoServiceProvider);
                }
            }

            using (var rsaCryptoServiceProvider = new RSAOpenSsl())
            {
                rsaCryptoServiceProvider.ImportParameters(rsaParameters);
                return RsaExtensions.ToXmlString(rsaCryptoServiceProvider);
            }
        }

        private string ExtractEcKeyInformation(Dictionary<string, object> information)
        {
            var xCoordinate = information.FirstOrDefault(i => i.Key == JwtConstants.JsonWebKeyParameterNames.EcKey.XCoordinateName);
            var yCoordinate = information.FirstOrDefault(i => i.Key == JwtConstants.JsonWebKeyParameterNames.EcKey.YCoordinateName);
            if (xCoordinate.IsDefault() || yCoordinate.IsDefault())
            {
                throw new InvalidOperationException(ErrorDescriptions.CannotExtractParametersFromJsonWebKey);
            }

            byte[] xCoordinateBytes, yCoordinateBytes;
            try
            {
                xCoordinateBytes = xCoordinate.Value.ToString().Base64DecodeBytes();
                yCoordinateBytes = yCoordinate.Value.ToString().Base64DecodeBytes();
            }
            catch (Exception)
            {
                throw new InvalidOperationException(ErrorDescriptions.OneOfTheParameterIsNotBase64Encoded);
            }

            var cngKeySerialized = new CngKeySerialized
            {
                X = xCoordinateBytes,
                Y = yCoordinateBytes
            };

            var serializer = new XmlSerializer(typeof(CngKeySerialized));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, cngKeySerialized);
                return writer.ToString();
            }
        }
    }
}
