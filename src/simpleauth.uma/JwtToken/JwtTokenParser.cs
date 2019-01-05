﻿//// Copyright © 2015 Habart Thierry, © 2018 Jacob Reimers
//// 
//// Licensed under the Apache License, Version 2.0 (the "License");
//// you may not use this file except in compliance with the License.
//// You may obtain a copy of the License at
//// 
////     http://www.apache.org/licenses/LICENSE-2.0
//// 
//// Unless required by applicable law or agreed to in writing, software
//// distributed under the License is distributed on an "AS IS" BASIS,
//// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//// See the License for the specific language governing permissions and
//// limitations under the License.

//namespace SimpleAuth.Uma.JwtToken
//{
//    using System;
//    using SimpleAuth;

//    internal class JwtTokenParser : IJwtTokenParser
//    {
//        private readonly IJwsParser _jwsParser;
//        private readonly IJsonWebKeyConverter _jsonWebKeyConverter;

//        public JwtTokenParser(
//            IJwsParser jwsParser,
//            IJsonWebKeyConverter jsonWebKeyConverter)
//        {
//            _jwsParser = jwsParser;
//            _jsonWebKeyConverter = jsonWebKeyConverter;
//        }

//        public JwtSecurityToken UnSign(string jws, string openidUrl, JsonWebKeySet jsonWebKeySet)
//        {
//            if (string.IsNullOrWhiteSpace(jws))
//            {
//                throw new ArgumentNullException(nameof(jws));
//            }

//            if (string.IsNullOrWhiteSpace(openidUrl))
//            {
//                throw new ArgumentNullException(nameof(openidUrl));
//            }

//            var protectedHeader = _jwsParser.GetHeader(jws);
//            if (protectedHeader == null)
//            {
//                return null;
//            }

//            var jsonWebKeys = _jsonWebKeyConverter.ExtractSerializedKeys(jsonWebKeySet);
//            if (jsonWebKeys == null ||
//                !jsonWebKeys.Any(j => j.Kid == protectedHeader.Kid))
//            {
//                return null;
//            }

//            var jsonWebKey = jsonWebKeys.First(j => j.Kid == protectedHeader.Kid);
//            if (protectedHeader.Alg == JwtConstants.JwsAlgNames.NONE)
//            {
//                return _jwsParser.GetPayload(jws);
//            }

//            return _jwsParser.ValidateSignature(jws, jsonWebKey);
//        }
//    }
//}
