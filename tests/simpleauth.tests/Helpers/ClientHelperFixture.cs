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

namespace SimpleAuth.Tests.Helpers
{
    using Moq;
    using Shared.Repositories;
    using SimpleAuth.Helpers;
    using SimpleAuth.JwtToken;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class ClientHelperFixture
    {
        private Mock<IClientStore> _clientRepositoryStub;
        private Mock<IJwtGenerator> _jwtGeneratorStub;
        private IClientHelper _clientHelper;

        [Fact]
        public async Task When_Passing_Null_Parameters_Then_Exception_Is_Thrown()
        {
            InitializeFakeObjects();

            await Assert.ThrowsAsync<ArgumentNullException>(() => _clientHelper.GenerateIdTokenAsync(string.Empty, null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => _clientHelper.GenerateIdTokenAsync("client_id", null)).ConfigureAwait(false);
        }

        //[Fact]
        //public async Task When_Signed_Response_Alg_Is_Not_Passed_Then_RS256_Is_Used()
        //{
        //    InitializeFakeObjects();
        //    var client = new Client();
        //    _clientRepositoryStub.Setup(c => c.GetById(It.IsAny<string>()))
        //        .Returns(Task.FromResult(client));

        //    await _clientHelper.GenerateIdTokenAsync("client_id", new JwtPayload()).ConfigureAwait(false);

        //    _jwtGeneratorStub.Verify(j => j.SignAsync(It.IsAny<JwtPayload>(), SecurityAlgorithms.RsaSha256));
        //}

        //[Fact]
        //public async Task When_Signed_Response_And_EncryptResponseAlg_Are_Passed_Then_EncryptResponseEnc_A128CBC_HS256_Is_Used()
        //{
        //    InitializeFakeObjects();
        //    var client = new Client
        //    {
        //        IdTokenSignedResponseAlg = SecurityAlgorithms.RsaSha256,
        //        IdTokenEncryptedResponseAlg = SecurityAlgorithms.RsaPKCS1
        //    };
        //    _clientRepositoryStub.Setup(c => c.GetById(It.IsAny<string>()))
        //        .Returns(Task.FromResult(client));

        //    await _clientHelper.GenerateIdTokenAsync("client_id", new JwtPayload()).ConfigureAwait(false);

        //    _jwtGeneratorStub.Verify(j => j.SignAsync(It.IsAny<JwtPayload>(), SecurityAlgorithms.RsaSha256));
        //    _jwtGeneratorStub.Verify(j => j.EncryptAsync(It.IsAny<JwtPayload>(), SecurityAlgorithms.RsaPKCS1, SecurityAlgorithms.Aes128CbcHmacSha256));
        //}

        //[Fact]
        //public async Task When_Sign_And_Encrypt_JwsPayload_Then_Functions_Are_Called()
        //{
        //    InitializeFakeObjects();
        //    var client = new Client
        //    {
        //        IdTokenSignedResponseAlg = SecurityAlgorithms.RsaSha256,
        //        IdTokenEncryptedResponseAlg = SecurityAlgorithms.RsaPKCS1,
        //        IdTokenEncryptedResponseEnc = SecurityAlgorithms.Aes128CbcHmacSha256
        //    };
        //    _clientRepositoryStub.Setup(c => c.GetById(It.IsAny<string>()))
        //        .Returns(Task.FromResult(client));

        //    await _clientHelper.GenerateIdTokenAsync("client_id", new JwtPayload()).ConfigureAwait(false);

        //    _jwtGeneratorStub.Verify(j => j.SignAsync(It.IsAny<JwtPayload>(), SecurityAlgorithms.RsaSha256));
        //    _jwtGeneratorStub.Verify(j => j.EncryptAsync(It.IsAny<JwtPayload>(), SecurityAlgorithms.RsaPKCS1, SecurityAlgorithms.Aes128CbcHmacSha256));
        //}

        private void InitializeFakeObjects()
        {
            _clientRepositoryStub = new Mock<IClientStore>();
            _jwtGeneratorStub = new Mock<IJwtGenerator>();
            _clientHelper = new ClientHelper(
                _clientRepositoryStub.Object,
                _jwtGeneratorStub.Object);
        }
    }
}
