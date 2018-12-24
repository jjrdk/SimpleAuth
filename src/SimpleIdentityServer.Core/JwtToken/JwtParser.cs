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

using SimpleIdentityServer.Core.Errors;
using SimpleIdentityServer.Core.Jwt.Converter;
using SimpleIdentityServer.Core.Jwt.Encrypt;
using SimpleIdentityServer.Core.Jwt.Signature;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Core.JwtToken
{
    using Json;
    using Shared;
    using Shared.Models;
    using Shared.Repositories;
    using Shared.Requests;
    using System.Net.Http;

    public class JwtParser : IJwtParser
    {
        private readonly IJweParser _jweParser;
        private readonly IJwsParser _jwsParser;
        private readonly HttpClient _httpClientFactory;
        private readonly IClientStore _clientRepository;
        private readonly IJsonWebKeyConverter _jsonWebKeyConverter;
        private readonly IJsonWebKeyRepository _jsonWebKeyRepository;

        public JwtParser(
            IJweParser jweParser,
            IJwsParser jwsParser,
            HttpClient httpClientFactory,
            IClientStore clientRepository,
            IJsonWebKeyConverter jsonWebKeyConverter,
            IJsonWebKeyRepository jsonWebKeyRepository)
        {
            _jweParser = jweParser;
            _jwsParser = jwsParser;
            _httpClientFactory = httpClientFactory;
            _clientRepository = clientRepository;
            _jsonWebKeyConverter = jsonWebKeyConverter;
            _jsonWebKeyRepository = jsonWebKeyRepository;
        }

        public bool IsJweToken(string jwe)
        {
            return _jweParser.GetHeader(jwe) != null;
        }

        public bool IsJwsToken(string jws)
        {
            return _jwsParser.GetHeader(jws) != null;
        }

        public async Task<string> DecryptAsync(string jwe)
        {
            if (string.IsNullOrWhiteSpace(jwe))
            {
                throw new ArgumentNullException("jwe");
            }

            var protectedHeader = _jweParser.GetHeader(jwe);
            if (protectedHeader == null)
            {
                return string.Empty;
            }

            var jsonWebKey = await _jsonWebKeyRepository.GetByKidAsync(protectedHeader.Kid).ConfigureAwait(false);
            if (jsonWebKey == null)
            {
                return string.Empty;
            }

            return _jweParser.Parse(jwe, jsonWebKey);
        }

        public async Task<string> DecryptAsync(string jwe, string clientId)
        {
            var jsonWebKey = await GetJsonWebKeyToDecrypt(jwe, clientId).ConfigureAwait(false);
            if (jsonWebKey == null)
            {
                return string.Empty;
            }

            return _jweParser.Parse(jwe, jsonWebKey);
        }

        public async Task<string> DecryptWithPasswordAsync(string jwe, string clientId, string password)
        {
            var jsonWebKey = await GetJsonWebKeyToDecrypt(jwe, clientId).ConfigureAwait(false);
            if (jsonWebKey == null)
            {
                return string.Empty;
            }

            return _jweParser.ParseByUsingSymmetricPassword(jwe, jsonWebKey, password);
        }

        public async Task<JwsPayload> UnSignAsync(string jws)
        {
            if (string.IsNullOrWhiteSpace(jws))
            {
                throw new ArgumentNullException(nameof(jws));
            }

            var protectedHeader = _jwsParser.GetHeader(jws);
            if (protectedHeader == null)
            {
                return null;
            }

            var jsonWebKey = await _jsonWebKeyRepository.GetByKidAsync(protectedHeader.Kid).ConfigureAwait(false);
            return UnSignWithJsonWebKey(jsonWebKey, protectedHeader, jws);
        }

        public async Task<JwsPayload> UnSignAsync(string jws, string clientId)
        {
            if (string.IsNullOrWhiteSpace(jws))
            {
                throw new ArgumentNullException(nameof(jws));
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            var client = await _clientRepository.GetById(clientId).ConfigureAwait(false);
            if (client == null)
            {
                throw new InvalidOperationException(string.Format(ErrorDescriptions.ClientIsNotValid, clientId));
            }

            var protectedHeader = _jwsParser.GetHeader(jws);
            if (protectedHeader == null)
            {
                return null;
            }

            var jsonWebKey = await GetJsonWebKeyFromClient(client, protectedHeader.Kid).ConfigureAwait(false);
            return UnSignWithJsonWebKey(jsonWebKey, protectedHeader, jws);
        }

        private JwsPayload UnSignWithJsonWebKey(JsonWebKey jsonWebKey, JwsProtectedHeader jwsProtectedHeader, string jws)
        {
            if (jsonWebKey == null
                && jwsProtectedHeader.Alg != Jwt.JwtConstants.JwsAlgNames.NONE)
            {
                return null;
            }

            if (jwsProtectedHeader.Alg == Jwt.JwtConstants.JwsAlgNames.NONE)
            {
                return _jwsParser.GetPayload(jws);
            }

            return _jwsParser.ValidateSignature(jws, jsonWebKey);
        }

        private async Task<JsonWebKey> GetJsonWebKeyToDecrypt(string jwe, string clientId)
        {
            if (string.IsNullOrWhiteSpace(jwe))
            {
                throw new ArgumentNullException(nameof(jwe));
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            var client = await _clientRepository.GetById(clientId).ConfigureAwait(false);
            if (client == null)
            {
                throw new InvalidOperationException(string.Format(ErrorDescriptions.ClientIsNotValid, clientId));
            }

            var protectedHeader = _jweParser.GetHeader(jwe);
            if (protectedHeader == null)
            {
                return null;
            }

            var jsonWebKey = await GetJsonWebKeyFromClient(client, protectedHeader.Kid).ConfigureAwait(false);
            return jsonWebKey;
        }

        private async Task<JsonWebKey> GetJsonWebKeyFromClient(Client client, string kid)
        {
            JsonWebKey result = null;
            // Fetch the json web key from the jwks_uri
            if (client.JwksUri != null)
            {
                try
                {
                    var request = await _httpClientFactory.GetAsync(client.JwksUri).ConfigureAwait(false);
                    request.EnsureSuccessStatusCode();
                    var json = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var jsonWebKeySet = json.DeserializeWithJavascript<JsonWebKeySet>();
                    var jsonWebKeys = _jsonWebKeyConverter.ExtractSerializedKeys(jsonWebKeySet);
                    return jsonWebKeys.FirstOrDefault(j => j.Kid == kid);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            // Fetch the json web key from the jwks
            if (client.JsonWebKeys != null &&
                client.JsonWebKeys.Any())
            {
                result = client.JsonWebKeys.FirstOrDefault(j => j.Kid == kid);
            }

            return result;
        }
    }
}
