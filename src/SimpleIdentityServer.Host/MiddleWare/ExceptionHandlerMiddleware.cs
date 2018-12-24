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

using Microsoft.AspNetCore.Http;
using SimpleIdentityServer.Core.Errors;
using SimpleIdentityServer.Core.Exceptions;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Host.MiddleWare
{
    using Json;
    using Shared.Responses;

    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ExceptionHandlerMiddlewareOptions _options;

        public ExceptionHandlerMiddleware(
            RequestDelegate next,
            ExceptionHandlerMiddlewareOptions options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                var simpleIdentityServerEventSource = _options.SimpleIdentityServerEventSource;
                var identityServerExceptionWithState = exception as IdentityServerExceptionWithState;
                if (!(exception is IdentityServerException identityServerException))
                {
                    identityServerException = new IdentityServerException(ErrorCodes.UnhandledExceptionCode, exception.Message);
                    simpleIdentityServerEventSource.Failure(exception);
                }
                else
                {
                    var code = identityServerException.Code;
                    var message = identityServerException.Message;
                    var state = identityServerExceptionWithState == null
                        ? string.Empty
                        : identityServerExceptionWithState.State;
                    simpleIdentityServerEventSource.OpenIdFailure(code, message, state);
                }

                context.Response.Clear();
                if (identityServerExceptionWithState != null)
                {
                    ErrorResponse errorResponseWithState = new ErrorResponseWithState
                    {
                        State = identityServerExceptionWithState.State
                    };

                    PopulateError(errorResponseWithState, identityServerExceptionWithState);
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.ContentType = "application/json";
                    var serializedError = errorResponseWithState.SerializeWithDataContract();
                    await context.Response.WriteAsync(serializedError).ConfigureAwait(false);
                }
                else
                {
                    var error = new ErrorResponse();
                    PopulateError(error, identityServerException);
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.ContentType = "application/json";
                    var serializedError = error.SerializeWithDataContract();
                    await context.Response.WriteAsync(serializedError).ConfigureAwait(false);
                }
            }
        }

        private static void PopulateError(ErrorResponse errorResponse, IdentityServerException exception)
        {
            errorResponse.Error = exception.Code;
            errorResponse.ErrorDescription = exception.Message;
        }
    }
}
