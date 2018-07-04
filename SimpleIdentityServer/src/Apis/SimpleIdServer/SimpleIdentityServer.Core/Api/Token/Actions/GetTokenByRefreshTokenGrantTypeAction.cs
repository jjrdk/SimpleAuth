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

using SimpleIdentityServer.Core.Common.Models;
using SimpleIdentityServer.Core.Errors;
using SimpleIdentityServer.Core.Exceptions;
using SimpleIdentityServer.Core.Helpers;
using SimpleIdentityServer.Core.JwtToken;
using SimpleIdentityServer.Core.Parameters;
using SimpleIdentityServer.OAuth.Logging;
using SimpleIdentityServer.Store;
using System;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Core.Api.Token.Actions
{
    public interface IGetTokenByRefreshTokenGrantTypeAction
    {
        Task<GrantedToken> Execute(RefreshTokenGrantTypeParameter refreshTokenGrantTypeParameter);
    }

    public sealed class GetTokenByRefreshTokenGrantTypeAction : IGetTokenByRefreshTokenGrantTypeAction
    {
        private readonly IClientHelper _clientHelper;
        private readonly IOAuthEventSource _oauthEventSource;
        private readonly IGrantedTokenGeneratorHelper _grantedTokenGeneratorHelper;
        private readonly ITokenStore _tokenStore;
        private readonly IJwtGenerator _jwtGenerator;

        public GetTokenByRefreshTokenGrantTypeAction(
            IClientHelper clientHelper,
            IOAuthEventSource oauthEventSource,
            IGrantedTokenGeneratorHelper grantedTokenGeneratorHelper,
            ITokenStore tokenStore,
            IJwtGenerator jwtGenerator)
        {
            _clientHelper = clientHelper;
            _oauthEventSource = oauthEventSource;
            _grantedTokenGeneratorHelper = grantedTokenGeneratorHelper;
            _tokenStore = tokenStore;
            _jwtGenerator = jwtGenerator;
        }

        public async Task<GrantedToken> Execute(RefreshTokenGrantTypeParameter refreshTokenGrantTypeParameter)
        {
            if (refreshTokenGrantTypeParameter == null)
            {
                throw new ArgumentNullException(nameof(refreshTokenGrantTypeParameter));
            }

            // 1. Validate parameters
            var grantedToken = await ValidateParameter(refreshTokenGrantTypeParameter);
            // 2. Generate a new access token & insert it
            var generatedToken = await _grantedTokenGeneratorHelper.GenerateTokenAsync(
                grantedToken.ClientId,
                grantedToken.Scope,
                grantedToken.UserInfoPayLoad,
                grantedToken.IdTokenPayLoad);
            generatedToken.ParentTokenId = grantedToken.Id;
            // 3. Fill-in the idtoken
            if (generatedToken.IdTokenPayLoad != null)
            {
                await _jwtGenerator.UpdatePayloadDate(generatedToken.IdTokenPayLoad);
                generatedToken.IdToken = await _clientHelper.GenerateIdTokenAsync(generatedToken.ClientId, generatedToken.IdTokenPayLoad);
            }

            await _tokenStore.AddToken(generatedToken);
            _oauthEventSource.GrantAccessToClient(generatedToken.ClientId,
                generatedToken.AccessToken,
                generatedToken.Scope);
            return generatedToken;
        }

        private async Task<GrantedToken> ValidateParameter(RefreshTokenGrantTypeParameter refreshTokenGrantTypeParameter)
        {
            var grantedToken = await _tokenStore.GetRefreshToken(refreshTokenGrantTypeParameter.RefreshToken);
            if (grantedToken == null)
            {
                throw new IdentityServerException(
                    ErrorCodes.InvalidGrant,
                    ErrorDescriptions.TheRefreshTokenIsNotValid);
            }

            return grantedToken;
        }
    }
}
