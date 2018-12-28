﻿//#region copyright
//// Copyright © 2015 Habart Thierry, © 2018 Jacob Reimers
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
//#endregion

//using SimpleIdentityServer.Core.Jwt.Serializer;
//using System;
//using System.Security.Cryptography;
//using System.Runtime.InteropServices;
//using Xunit;

//namespace SimpleIdentityServer.Core.Jwt.UnitTests.Serializer
//{
//    public sealed class CngKeySerializerFixture
//    {
//        private ICngKeySerializer _cngKeySerializer;

//#region Serialize with private key

//        [Fact]
//        public void When_Passing_Null_To_Serialize_With_Private_Key_Then_Exception_Is_Thrown()
//        {
////            InitializeFakeObjects();

//            //            Assert.Throws<ArgumentNullException>(() => _cngKeySerializer.SerializeCngKeyWithPrivateKey(null));
//        }

//        [Fact]
//        public void When_Private_Key_Cannot_Be_Extracted_Then_Exception_Is_Thrown()
//        {
////            InitializeFakeObjects();
//            var keyCreationParameter = new CngKeyCreationParameters
//            {
//                ExportPolicy = CngExportPolicies.None
//            };

//            var cnk = CngKey.Create(CngAlgorithm.ECDsaP256, null, keyCreationParameter);
//            var isExceptionRaised = false;
//            try
//            {
//                //                _cngKeySerializer.SerializeCngKeyWithPrivateKey(cnk);
//            }
//            catch (CryptographicException)
//            {
//                isExceptionRaised = true;
//            }

//            //            Assert.True(isExceptionRaised);
//        }

//        [Fact]
//        public void When_Serialize_The_CngKey_With_Private_Key_Then_String_Is_Returned()
//        {
////            InitializeFakeObjects();
//            var keyCreationParameter = new CngKeyCreationParameters
//            {
//                ExportPolicy = CngExportPolicies.AllowPlaintextExport
//            };

//            var cnk = CngKey.Create(CngAlgorithm.ECDsaP256, null, keyCreationParameter);

//            //            var result = _cngKeySerializer.SerializeCngKeyWithPrivateKey(cnk);

//            //            Assert.NotNull(result);
//        }

//#endregion

//#region Deserialize with private key

//        [Fact]
//        public void When_Passing_Null_To_Deserialize_Function_With_Private_Key_Then_Exception_Is_Thrown()
//        {
////            InitializeFakeObjects();

//            //            Assert.Throws<ArgumentNullException>(() => _cngKeySerializer.DeserializeCngKeyWithPrivateKey(null));
//        }

//        [Fact]
//        public void When_Deserialize_Xml_With_Private_Key_Then_CngKey_Is_Returned()
//        {            
////            InitializeFakeObjects();
//            var keyCreationParameter = new CngKeyCreationParameters
//            {
//                ExportPolicy = CngExportPolicies.AllowPlaintextExport
//            };

//            var cnk = CngKey.Create(CngAlgorithm.ECDsaP256, null, keyCreationParameter);
//            var serializedXml = _cngKeySerializer.SerializeCngKeyWithPrivateKey(cnk);

//            //            var result = _cngKeySerializer.DeserializeCngKeyWithPrivateKey(serializedXml);

//            //            Assert.NotNull(result);
//        }

//#endregion

//#region Serialize with public key

//        [Fact]
//        public void When_Passing_Null_To_Serialize_With_Public_Key_Then_Exception_Is_Thrown()
//        {
////            InitializeFakeObjects();

//            //            Assert.Throws<ArgumentNullException>(() => _cngKeySerializer.SerializeCngKeyWithPublicKey(null));
//        }

//        [Fact]
//        public void When_Serialize_The_CngKey_With_Public_Key_Then_String_Is_Returned()
//        {
////            InitializeFakeObjects();
//            var keyCreationParameter = new CngKeyCreationParameters
//            {
//                ExportPolicy = CngExportPolicies.AllowPlaintextExport
//            };

//            var cnk = CngKey.Create(CngAlgorithm.ECDsaP256, null, keyCreationParameter);

//            //            var result = _cngKeySerializer.SerializeCngKeyWithPublicKey(cnk);

//            //            Assert.NotNull(result);
//        }

//#endregion

//#region Deserialize with public key

//        [Fact]
//        public void When_Passing_Null_Parameter_To_Deserialize_With_Public_Key_Then_Exception_Is_Thrown()
//        {
////            InitializeFakeObjects();

//            //            Assert.Throws<ArgumentNullException>(() => _cngKeySerializer.DeserializeCngKeyWithPublicKey(null));
//        }

//        [Fact]
//        public void When_Deserialize_With_Public_Key_Then_String_Is_Returned()
//        {
////            InitializeFakeObjects();
            
//            var keyCreationParameter = new CngKeyCreationParameters
//            {
//                ExportPolicy = CngExportPolicies.None
//            };

//            var cnk = CngKey.Create(CngAlgorithm.ECDsaP521, null, keyCreationParameter);

//            var serialized = _cngKeySerializer.SerializeCngKeyWithPublicKey(cnk);

//            //            var result = _cngKeySerializer.DeserializeCngKeyWithPublicKey(serialized);

//            //            Assert.NotNull(result);
//        }

//#endregion

//        private void InitializeFakeObjects()
//        {
//            _cngKeySerializer = new CngKeySerializer();
//        }
//    }
//}