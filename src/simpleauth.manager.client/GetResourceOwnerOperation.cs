﻿namespace SimpleAuth.Manager.Client
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Models;

    internal sealed class GetResourceOwnerOperation
    {
        private readonly HttpClient _httpClient;

        public GetResourceOwnerOperation(HttpClient httpClientFactory)
        {
            _httpClient = httpClientFactory;
        }

        public async Task<GenericResponse<ResourceOwner>> Execute(
            Uri clientsUri,
            string authorizationHeaderValue = null)
        {
            if (clientsUri == null)
            {
                throw new ArgumentNullException(nameof(clientsUri));
            }

            var request = new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = clientsUri };
            if (!string.IsNullOrWhiteSpace(authorizationHeaderValue))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorizationHeaderValue);
            }

            var httpResult = await _httpClient.SendAsync(request).ConfigureAwait(false);
            var content = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!httpResult.IsSuccessStatusCode)
            {
                return new GenericResponse<ResourceOwner>
                {
                    ContainsError = true,
                    Error = Serializer.Default.Deserialize<ErrorDetails>(content),
                    HttpStatus = httpResult.StatusCode
                };
            }

            return new GenericResponse<ResourceOwner>
            {
                Content = Serializer.Default.Deserialize<ResourceOwner>(content)
            };
        }
    }
}
