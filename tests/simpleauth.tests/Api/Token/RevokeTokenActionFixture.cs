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

namespace SimpleAuth.Tests.Api.Token
{
    using Errors;
    using Exceptions;
    using Moq;
    using Parameters;
    using Shared.Models;
    using SimpleAuth;
    using SimpleAuth.Api.Token.Actions;
    using SimpleAuth.Authenticate;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    public class RevokeTokenActionFixture
    {
        private Mock<IAuthenticateClient> _authenticateClientStub;
        private Mock<ITokenStore> _grantedTokenRepositoryStub;
        private RevokeTokenAction _revokeTokenAction;

        [Fact]
        public async Task When_Passing_Null_Parameter_Then_Exceptions_Are_Thrown()
        {
            InitializeFakeObjects();

            await Assert.ThrowsAsync<ArgumentNullException>(() => _revokeTokenAction.Execute(null, null, null, null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => _revokeTokenAction.Execute(new RevokeTokenParameter(), null, null, null)).ConfigureAwait(false);
        }

        [Fact]
        public async Task When_Client_Does_Not_Exist_Then_Exception_Is_Thrown()
        {
            InitializeFakeObjects();
            var parameter = new RevokeTokenParameter
            {
                Token = "access_token"
            };

            _authenticateClientStub.Setup(a => a.AuthenticateAsync(It.IsAny<AuthenticateInstruction>(), null))
                .Returns(() => Task.FromResult(new AuthenticationResult(null, null)));

            var exception = await Assert.ThrowsAsync<SimpleAuthException>(() => _revokeTokenAction.Execute(parameter, null, null, null)).ConfigureAwait(false);
            Assert.NotNull(exception);
            Assert.Equal(ErrorCodes.InvalidClient, exception.Code);
        }

        [Fact]
        public async Task When_Token_Does_Not_Exist_Then_Exception_Is_Returned()
        {
            InitializeFakeObjects();
            var parameter = new RevokeTokenParameter
            {
                Token = "access_token"
            };

            _authenticateClientStub.Setup(a => a.AuthenticateAsync(It.IsAny<AuthenticateInstruction>(), null))
                .Returns(() => Task.FromResult(new AuthenticationResult(new Client(), null)));
            _grantedTokenRepositoryStub.Setup(g => g.GetAccessToken(It.IsAny<string>()))
                .Returns(() => Task.FromResult((GrantedToken)null));
            _grantedTokenRepositoryStub.Setup(g => g.GetRefreshToken(It.IsAny<string>()))
                .Returns(() => Task.FromResult((GrantedToken)null));

            var result = await Assert.ThrowsAsync<SimpleAuthException>(() => _revokeTokenAction.Execute(parameter, null, null, null)).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal("invalid_token", result.Code);
        }

        [Fact]
        public async Task When_Invalidating_Refresh_Token_Then_GrantedTokenChildren_Are_Removed()
        {
            InitializeFakeObjects();
            var parent = new GrantedToken
            {
                RefreshToken = "refresh_token"
            };
            var child = new GrantedToken
            {
                ParentTokenId = "refresh_token",
                AccessToken = "access_token_child"
            };
            var parameter = new RevokeTokenParameter
            {
                Token = "refresh_token"
            };

            _authenticateClientStub.Setup(a => a.AuthenticateAsync(It.IsAny<AuthenticateInstruction>(), null))
                .Returns(() => Task.FromResult(new AuthenticationResult(new Client(), null)));
            _grantedTokenRepositoryStub.Setup(g => g.GetAccessToken(It.IsAny<string>()))
                .Returns(() => Task.FromResult((GrantedToken)null));
            _grantedTokenRepositoryStub.Setup(g => g.GetRefreshToken(It.IsAny<string>()))
                .Returns(Task.FromResult(parent));
            _grantedTokenRepositoryStub.Setup(g => g.RemoveAccessToken(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            await _revokeTokenAction.Execute(parameter, null, null, null).ConfigureAwait(false);

            _grantedTokenRepositoryStub.Verify(g => g.RemoveRefreshToken(parent.RefreshToken));
        }

        [Fact]
        public async Task When_Invalidating_Access_Token_Then_GrantedToken_Is_Removed()
        {
            InitializeFakeObjects();
            var grantedToken = new GrantedToken
            {
                AccessToken = "access_token"
            };
            var parameter = new RevokeTokenParameter
            {
                Token = "access_token"
            };

            _authenticateClientStub.Setup(a => a.AuthenticateAsync(It.IsAny<AuthenticateInstruction>(), null))
                .Returns(() => Task.FromResult(new AuthenticationResult(new Client(), null)));
            _grantedTokenRepositoryStub.Setup(g => g.GetAccessToken(It.IsAny<string>()))
                .Returns(Task.FromResult(grantedToken));
            _grantedTokenRepositoryStub.Setup(g => g.GetRefreshToken(It.IsAny<string>()))
                .Returns(() => Task.FromResult((GrantedToken)null));
            _grantedTokenRepositoryStub.Setup(g => g.RemoveAccessToken(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            await _revokeTokenAction.Execute(parameter, null, null, null).ConfigureAwait(false);

            _grantedTokenRepositoryStub.Verify(g => g.RemoveAccessToken(grantedToken.AccessToken));
        }

        private void InitializeFakeObjects()
        {
            _authenticateClientStub = new Mock<IAuthenticateClient>();
            _grantedTokenRepositoryStub = new Mock<ITokenStore>();
            _revokeTokenAction = new RevokeTokenAction(
                _authenticateClientStub.Object,
                _grantedTokenRepositoryStub.Object);
        }
    }
}
