﻿// Copyright © 2018 Habart Thierry, © 2018 Jacob Reimers
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

namespace SimpleAuth.Client
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Models;
    using SimpleAuth.Shared.Requests;
    using SimpleAuth.Shared.Responses;

    /// <summary>
    /// Defines the UMA client.
    /// </summary>
    public class UmaClient : IUmaPermissionClient, IResourceClient
    {
        private const string JsonMimeType = "application/json";
        private readonly HttpClient _client;
        private readonly Uri _configurationUri;
        private UmaConfiguration _umaConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="UmaClient"/> class.
        /// </summary>
        /// <param name="client">The <see cref="HttpClient"/> to use for requests.</param>
        /// <param name="authorityUri">The <see cref="Uri"/> of the UMA authority.</param>
        public UmaClient(HttpClient client, Uri authorityUri)
        {
            var builder = new UriBuilder(
                authorityUri.Scheme,
                authorityUri.Host,
                authorityUri.Port,
                "/.well-known/uma2-configuration");
            _client = client;
            _configurationUri = builder.Uri;
        }

        /// <summary>
        /// Executes the specified introspection request.
        /// </summary>
        /// <param name="introspectionRequest">The introspection request.</param>
        /// <returns></returns>
        public async Task<GenericResponse<UmaIntrospectionResponse>> Introspect(IntrospectionRequest introspectionRequest)
        {
            var discoveryInformation = await GetUmaConfiguration().ConfigureAwait(false);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = new FormUrlEncodedContent(introspectionRequest),
                RequestUri = discoveryInformation.IntrospectionEndpoint
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", introspectionRequest.PatToken);

            var result = await _client.SendAsync(request).ConfigureAwait(false);
            var json = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!result.IsSuccessStatusCode)
            {
                var error = Serializer.Default.Deserialize<ErrorDetails>(json);
                return new GenericResponse<UmaIntrospectionResponse>
                {
                    Error = error,
                    StatusCode = result.StatusCode
                };
            }

            return new GenericResponse<UmaIntrospectionResponse>
            {
                StatusCode = result.StatusCode,
                Content = Serializer.Default.Deserialize<UmaIntrospectionResponse>(json)
            };
        }

        /// <inheritdoc />
        public async Task<GenericResponse<PermissionResponse>> RequestPermission(
            string token,
            CancellationToken cancellationToken = default,
            params PermissionRequest[] requests)
        {
            if (requests == null)
            {
                throw new ArgumentNullException(nameof(requests));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            var configuration = await GetUmaConfiguration().ConfigureAwait(false);
            var url = configuration.PermissionEndpoint.AbsoluteUri;

            if (requests.Length > 1)
            {
                url += url.EndsWith("/") ? "bulk" : "/bulk";
            }

            var serializedPostPermission = requests.Length > 1
                ? Serializer.Default.Serialize(requests)
                : Serializer.Default.Serialize(requests[0]);
            var body = new StringContent(serializedPostPermission, Encoding.UTF8, JsonMimeType);
            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = body,
                RequestUri = new Uri(url)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerConstants.BearerScheme, token);
            var result = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!result.IsSuccessStatusCode)
            {
                return new GenericResponse<PermissionResponse>
                {
                    StatusCode = result.StatusCode,
                    Error = Serializer.Default.Deserialize<ErrorDetails>(content)
                };
            }

            return new GenericResponse<PermissionResponse>
            {
                StatusCode = result.StatusCode,
                Content = Serializer.Default.Deserialize<PermissionResponse>(content)
            };
        }

        /// <inheritdoc />
        public async Task<GenericResponse<UpdateResourceSetResponse>> UpdateResource(
            ResourceSet request,
            string token,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            var configuration = await GetUmaConfiguration().ConfigureAwait(false);
            var serializedPostResourceSet = Serializer.Default.Serialize(request);
            var body = new StringContent(serializedPostResourceSet, Encoding.UTF8, JsonMimeType);
            var httpRequest = new HttpRequestMessage
            {
                Content = body,
                Method = HttpMethod.Put,
                RequestUri = configuration.ResourceRegistrationEndpoint
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerConstants.BearerScheme, token);
            var httpResult = await _client.SendAsync(httpRequest).ConfigureAwait(false);
            var content = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!httpResult.IsSuccessStatusCode)
            {
                return new GenericResponse<UpdateResourceSetResponse>
                {
                    Error = Serializer.Default.Deserialize<ErrorDetails>(content),
                    StatusCode = httpResult.StatusCode
                };
            }

            return new GenericResponse<UpdateResourceSetResponse>
            {
                StatusCode = httpResult.StatusCode,
                Content = Serializer.Default.Deserialize<UpdateResourceSetResponse>(content)
            };
        }

        /// <inheritdoc />
        public async Task<GenericResponse<AddResourceSetResponse>> AddResource(
            ResourceSet request,
            string token,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            var serializedPostResourceSet = Serializer.Default.Serialize(request);
            var body = new StringContent(serializedPostResourceSet, Encoding.UTF8, JsonMimeType);
            var umaConfiguration = await GetUmaConfiguration().ConfigureAwait(false);
            var httpRequest = new HttpRequestMessage
            {
                Content = body,
                Method = HttpMethod.Post,
                RequestUri = umaConfiguration.ResourceRegistrationEndpoint
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerConstants.BearerScheme, token);

            var httpResult = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var content = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!httpResult.IsSuccessStatusCode)
            {
                return new GenericResponse<AddResourceSetResponse>
                {
                    Error = Serializer.Default.Deserialize<ErrorDetails>(content),
                    StatusCode = httpResult.StatusCode
                };
            }

            return new GenericResponse<AddResourceSetResponse>
            {
                StatusCode = httpResult.StatusCode,
                Content = Serializer.Default.Deserialize<AddResourceSetResponse>(content)
            };
        }

        /// <inheritdoc />
        public async Task<GenericResponse<object>> DeleteResource(
            string resourceSetId,
            string token,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(resourceSetId))
            {
                throw new ArgumentNullException(nameof(resourceSetId));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Invalid token", nameof(token));
            }

            var configuration = await GetUmaConfiguration().ConfigureAwait(false);
            var resourceSetUrl = configuration.ResourceRegistrationEndpoint.AbsoluteUri;
            resourceSetUrl += resourceSetUrl.EndsWith("/") ? resourceSetId : "/" + resourceSetId;

            var request = new HttpRequestMessage { Method = HttpMethod.Delete, RequestUri = new Uri(resourceSetUrl) };
            request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerConstants.BearerScheme, token);
            var httpResult = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var content = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!httpResult.IsSuccessStatusCode)
            {
                return new GenericResponse<object>
                {
                    Error = Serializer.Default.Deserialize<ErrorDetails>(content),
                    StatusCode = httpResult.StatusCode
                };
            }

            return new GenericResponse<object> { StatusCode = httpResult.StatusCode, };
        }

        /// <inheritdoc />
        public async Task<GenericResponse<string[]>> GetAllResources(
            string token,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Invalid token", nameof(token));
            }

            var configuration = await GetUmaConfiguration().ConfigureAwait(false);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = configuration.ResourceRegistrationEndpoint
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerConstants.BearerScheme, token);
            var httpResult = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!httpResult.IsSuccessStatusCode)
            {
                return new GenericResponse<string[]>
                {
                    Error = Serializer.Default.Deserialize<ErrorDetails>(json),
                    StatusCode = httpResult.StatusCode
                };
            }

            return new GenericResponse<string[]>
            {
                StatusCode = httpResult.StatusCode,
                Content = Serializer.Default.Deserialize<string[]>(json)
            };
        }

        /// <inheritdoc />
        public async Task<GenericResponse<ResourceSet>> GetResource(
            string resourceSetId,
            string token,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(resourceSetId))
            {
                throw new ArgumentException(nameof(resourceSetId));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Invalid token", nameof(token));
            }

            var configuration = await GetUmaConfiguration().ConfigureAwait(false);
            var resourceSetUrl = configuration.ResourceRegistrationEndpoint.AbsoluteUri;

            resourceSetUrl += resourceSetUrl.EndsWith("/") ? resourceSetId : "/" + resourceSetId;

            var request = new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = new Uri(resourceSetUrl) };
            request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerConstants.BearerScheme, token);
            var httpResult = await _client.SendAsync(request).ConfigureAwait(false);
            var json = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!httpResult.IsSuccessStatusCode)
            {
                return new GenericResponse<ResourceSet>()
                {
                    Error = Serializer.Default.Deserialize<ErrorDetails>(json),
                    StatusCode = httpResult.StatusCode
                };
            }

            return new GenericResponse<ResourceSet>()
            {
                StatusCode = httpResult.StatusCode,
                Content = Serializer.Default.Deserialize<ResourceSet>(json)
            };
        }

        /// <inheritdoc />
        public async Task<GenericResponse<PagedResult<ResourceSet>>> SearchResources(
            SearchResourceSet parameter,
            string token,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Invalid token", nameof(token));
            }

            var configuration = await GetUmaConfiguration().ConfigureAwait(false);
            var url = configuration.ResourceRegistrationEndpoint + "/.search";

            var serializedPostPermission = Serializer.Default.Serialize(parameter);
            var body = new StringContent(serializedPostPermission, Encoding.UTF8, JsonMimeType);
            var request = new HttpRequestMessage { Method = HttpMethod.Post, RequestUri = new Uri(url), Content = body };
            request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerConstants.BearerScheme, token);

            var httpResult = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var content = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!httpResult.IsSuccessStatusCode)
            {
                return new GenericResponse<PagedResult<ResourceSet>>()
                {
                    Error = Serializer.Default.Deserialize<ErrorDetails>(content),
                    StatusCode = httpResult.StatusCode
                };
            }

            return new GenericResponse<PagedResult<ResourceSet>>
            {
                StatusCode = httpResult.StatusCode,
                Content = Serializer.Default.Deserialize<PagedResult<ResourceSet>>(content)
            };
        }

        private async Task<UmaConfiguration> GetUmaConfiguration()
        {
            if (_umaConfiguration != null)
            {
                return _umaConfiguration;
            }

            var result = await _client.GetStringAsync(_configurationUri).ConfigureAwait(false);
            _umaConfiguration = Serializer.Default.Deserialize<UmaConfiguration>(result);

            return _umaConfiguration;
        }
    }
}
