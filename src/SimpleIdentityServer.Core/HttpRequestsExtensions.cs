﻿namespace SimpleIdentityServer.Core
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public static class HttpRequestsExtensions
    {
        public static string GetAbsoluteUriWithVirtualPath(this IHttpContextAccessor context)
        {
            var request = context.HttpContext.Request;
            var result = request.GetAbsoluteUriWithVirtualPath();
            return result;
        }

        public static string GetAbsoluteUriWithVirtualPath(this HttpRequest requestMessage)
        {
            var host = requestMessage.Host.Value;
            var http = "http://";
            if (requestMessage.IsHttps)
            {
                http = "https://";
            }

            var relativePath = requestMessage.PathBase.Value;
            return http + host + relativePath;
        }

        public static async Task<string> ReadAsStringAsync(this HttpRequest request)
        {
            request.Body.Position = 0;
            using (var reader = new StreamReader(request.Body))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}