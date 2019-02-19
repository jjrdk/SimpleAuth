﻿namespace SimpleAuth.Manager.Client
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Responses;

    internal sealed class GetAllResourceOwnersOperation
    {
        private readonly HttpClient _httpClient;

        public GetAllResourceOwnersOperation(HttpClient httpClientFactory)
        {
            _httpClient = httpClientFactory;
        }

        public async Task<GenericResponse<ResourceOwnerResponse[]>> Execute(
            Uri resourceOwnerUri,
            string authorizationHeaderValue = null)
        {
            if (resourceOwnerUri == null)
            {
                throw new ArgumentNullException(nameof(resourceOwnerUri));
            }

            var request = new HttpRequestMessage {Method = HttpMethod.Get, RequestUri = resourceOwnerUri};
            if (!string.IsNullOrWhiteSpace(authorizationHeaderValue))
            {
                request.Headers.Add("Authorization", "Bearer " + authorizationHeaderValue);
            }

            var httpResult = await _httpClient.SendAsync(request).ConfigureAwait(false);
            var content = await httpResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                httpResult.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                return new GenericResponse<ResourceOwnerResponse[]>
                {
                    ContainsError = true,
                    Error = JsonConvert.DeserializeObject<ErrorResponse>(content),
                    HttpStatus = httpResult.StatusCode
                };
            }

            return new GenericResponse<ResourceOwnerResponse[]>
            {
                Content = JsonConvert.DeserializeObject<ResourceOwnerResponse[]>(content)
            };
        }
    }
}
