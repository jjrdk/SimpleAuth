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

using Moq;
using SimpleIdentityServer.Core.Api.Jwks.Actions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SimpleIdentityServer.Core.UnitTests.Api.Jwks.Operations
{
    using SimpleAuth.Jwt;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Repositories;

    public sealed class RotateJsonWebKeysOperationFixture
    {
        private Mock<IJsonWebKeyRepository> _jsonWebKeyRepositoryStub;
        private Mock<ITokenStore> _tokenStoreStub;
        private IRotateJsonWebKeysOperation _rotateJsonWebKeysOperation;

        [Fact]
        public async Task When_There_Is_No_JsonWebKeys_To_Rotate_Then_False_Is_Returned()
        {            InitializeFakeObjects();
            _jsonWebKeyRepositoryStub.Setup(j => j.GetAllAsync())
                .Returns(() => Task.FromResult((ICollection<JsonWebKey>)null));

                        var result = await _rotateJsonWebKeysOperation.Execute().ConfigureAwait(false);

                        Assert.False(result);
        }

        [Fact]
        public async Task When_Rotating_Two_JsonWebKeys_Then_SerializedKeyProperty_Has_Changed()
        {            InitializeFakeObjects();
            const string firstJsonWebKeySerializedKey = "firstJsonWebKeySerializedKey";
            const string secondJsonWebKeySerializedKey = "secondJsonWebKeySerializedKey";
            ICollection<JsonWebKey> jsonWebKeys = new List<JsonWebKey>
            {
                new JsonWebKey
                {
                    Kid = "1",
                    SerializedKey = firstJsonWebKeySerializedKey
                },
                new JsonWebKey
                {
                    Kid = "2",
                    SerializedKey = secondJsonWebKeySerializedKey
                }
            };
            _jsonWebKeyRepositoryStub.Setup(j => j.GetAllAsync())
                .Returns(() => Task.FromResult(jsonWebKeys));

                        var result = await _rotateJsonWebKeysOperation.Execute().ConfigureAwait(false);

                        _jsonWebKeyRepositoryStub.Verify(j => j.UpdateAsync(It.IsAny<JsonWebKey>()));
            Assert.True(result);
        }

        private void InitializeFakeObjects()
        {
            _jsonWebKeyRepositoryStub = new Mock<IJsonWebKeyRepository>();
            _tokenStoreStub = new Mock<ITokenStore>();
            _rotateJsonWebKeysOperation = new RotateJsonWebKeysOperation(_jsonWebKeyRepositoryStub.Object, _tokenStoreStub.Object);
        }
    }
}
