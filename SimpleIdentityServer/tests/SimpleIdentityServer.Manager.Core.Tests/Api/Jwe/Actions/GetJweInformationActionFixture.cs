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

using Moq;
using SimpleIdentityServer.Core.Jwt;
using SimpleIdentityServer.Core.Jwt.Encrypt;
using SimpleIdentityServer.Core.Jwt.Signature;
using SimpleIdentityServer.Manager.Core.Api.Jwe.Actions;
using SimpleIdentityServer.Manager.Core.Errors;
using SimpleIdentityServer.Manager.Core.Exceptions;
using SimpleIdentityServer.Manager.Core.Helpers;
using SimpleIdentityServer.Manager.Core.Parameters;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SimpleIdentityServer.Manager.Core.Tests.Api.Jwe.Actions
{
    public class GetJweInformationActionFixture
    {
        private Mock<IJweParser> _jweParserStub;

        private Mock<IJwsParser> _jwsParserStub;

        private Mock<IJsonWebKeyHelper> _jsonWebKeyHelperStub;

        private IGetJweInformationAction _getJweInformationAction;

        #region Exceptions

        [Fact]
        public void When_Passing_Null_Parameter_Then_Exception_Are_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();
            var getJweParameter = new GetJweParameter();
            var getJweParameterWithJwe = new GetJweParameter
            {
                Jwe = "jwe"
            };

            // ACT & ASSERTS
            Assert.ThrowsAsync<ArgumentNullException>(() => _getJweInformationAction.ExecuteAsync(null));
            Assert.ThrowsAsync<ArgumentNullException>(() => _getJweInformationAction.ExecuteAsync(getJweParameter));
            Assert.ThrowsAsync<ArgumentNullException>(() => _getJweInformationAction.ExecuteAsync(getJweParameterWithJwe));
        }

        [Fact]
        public void When_Url_Is_Not_Well_Formed_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();
            const string url = "not_well_formed";
            var getJweParameter = new GetJweParameter
            {
                Jwe = "jwe",
                Url = url
            };

            // ACT & ASSERTS
            var content = Assert.ThrowsAsync<IdentityServerManagerException>(() => _getJweInformationAction.ExecuteAsync(getJweParameter));
            var exception = content.Result;
            Assert.True(exception.Code == ErrorCodes.InvalidRequestCode);
            Assert.True(exception.Message == string.Format(ErrorDescriptions.TheUrlIsNotWellFormed, url));
        }

        [Fact]
        public void When_Header_Is_Not_Correct_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();
            var getJweParameter = new GetJweParameter
            {
                Jwe = "jwe",
                Url = "http://google.be"
            };
            _jweParserStub.Setup(j => j.GetHeader(It.IsAny<string>()))
                .Returns(() => null);

            // ACT & ASSERTS
            var content = Assert.ThrowsAsync<IdentityServerManagerException>(() => _getJweInformationAction.ExecuteAsync(getJweParameter));
            var exception = content.Result;
            Assert.True(exception.Code == ErrorCodes.InvalidRequestCode);
            Assert.True(exception.Message == ErrorDescriptions.TheTokenIsNotAValidJwe);
        }

        [Fact]
        public void When_JsonWebKey_Cannot_Be_Extracted_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();
            var jweProtectedHeader = new JweProtectedHeader
            {
                Kid = "kid"
            };
            var getJweParameter = new GetJweParameter
            {
                Jwe = "jwe",
                Url = "http://google.be/"
            };
            _jweParserStub.Setup(j => j.GetHeader(It.IsAny<string>()))
                .Returns(jweProtectedHeader);
            _jsonWebKeyHelperStub.Setup(j => j.GetJsonWebKey(It.IsAny<string>(), It.IsAny<Uri>()))
                .Returns(Task.FromResult<JsonWebKey>(null));

            // ACT & ASSERTS
            var content = Assert.ThrowsAsync<IdentityServerManagerException>(() => _getJweInformationAction.ExecuteAsync(getJweParameter));
            var exception = content.Result;
            Assert.True(exception.Code == ErrorCodes.InvalidRequestCode);
            Assert.True(exception.Message == string.Format(ErrorDescriptions.TheJsonWebKeyCannotBeFound, jweProtectedHeader.Kid, getJweParameter.Url));
        }

        [Fact]
        public void When_No_Content_Can_Be_Extracted_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();
            var jweProtectedHeader = new JweProtectedHeader
            {
                Kid = "kid"
            };
            var jsonWebKey = new JsonWebKey();
            var getJweParameter = new GetJweParameter
            {
                Jwe = "jwe",
                Url = "http://google.be/"
            };
            _jweParserStub.Setup(j => j.GetHeader(It.IsAny<string>()))
                .Returns(jweProtectedHeader);
            _jsonWebKeyHelperStub.Setup(j => j.GetJsonWebKey(It.IsAny<string>(), It.IsAny<Uri>()))
                .Returns(Task.FromResult<JsonWebKey>(jsonWebKey));
            _jweParserStub.Setup(j => j.Parse(It.IsAny<string>(), It.IsAny<JsonWebKey>()))
                .Returns(string.Empty);

            // ACT & ASSERTS
            var content = Assert.ThrowsAsync<IdentityServerManagerException>(() => _getJweInformationAction.ExecuteAsync(getJweParameter));
            var exception = content.Result;
            Assert.True(exception.Code == ErrorCodes.InvalidRequestCode);
            Assert.True(exception.Message == ErrorDescriptions.TheContentCannotBeExtractedFromJweToken);
        }

        #endregion

        #region Happys paths

        [Fact]
        public void When_Decrypting_Jwe_With_Symmetric_Key_Then_Result_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();
            const string content = "jws";
            var jweProtectedHeader = new JweProtectedHeader
            {
                Kid = "kid"
            };
            var jwsProtectedHeader = new JwsProtectedHeader();
            var jsonWebKey = new JsonWebKey();
            var getJweParameter = new GetJweParameter
            {
                Jwe = "jwe",
                Url = "http://google.be/",
                Password = "password"
            };
            _jweParserStub.Setup(j => j.GetHeader(It.IsAny<string>()))
                .Returns(jweProtectedHeader);
            _jsonWebKeyHelperStub.Setup(j => j.GetJsonWebKey(It.IsAny<string>(), It.IsAny<Uri>()))
                .Returns(Task.FromResult<JsonWebKey>(jsonWebKey));
            _jweParserStub.Setup(j => j.ParseByUsingSymmetricPassword(It.IsAny<string>(), It.IsAny<JsonWebKey>(), It.IsAny<string>()))
                .Returns(content);
            _jwsParserStub.Setup(j => j.GetHeader(It.IsAny<string>()))
                .Returns(jwsProtectedHeader);

            // ACT & ASSERTS
            var result = _getJweInformationAction.ExecuteAsync(getJweParameter).Result;
            Assert.True(result.IsContentJws);
            Assert.True(result.Content == content);
        }

        #endregion

        private void InitializeFakeObjects()
        {
            _jweParserStub = new Mock<IJweParser>();
            _jwsParserStub = new Mock<IJwsParser>();
            _jsonWebKeyHelperStub = new Mock<IJsonWebKeyHelper>();
            _getJweInformationAction = new GetJweInformationAction(
                _jweParserStub.Object,
                _jwsParserStub.Object,
                _jsonWebKeyHelperStub.Object);
        }
    }
}
