﻿namespace SimpleAuth.Stores.Marten
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Marten;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Errors;
    using SimpleAuth.Shared.Models;
    using SimpleAuth.Shared.Repositories;
    using SimpleAuth.Shared.Requests;
    using SimpleAuth.Shared.Responses;

    /// <summary>
    /// Defines the Marten device authorization store.
    /// </summary>
    public class MartenDeviceAuthorizationStore : IDeviceAuthorizationStore
    {
        private readonly Func<IDocumentSession> _sessionFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="MartenDeviceAuthorizationStore"/> class.
        /// </summary>
        /// <param name="sessionFunc">The session factory.</param>
        public MartenDeviceAuthorizationStore(Func<IDocumentSession> sessionFunc)
        {
            _sessionFunc = sessionFunc;
        }

        /// <inheritdoc />
        public async Task<Option<DeviceAuthorizationResponse>> Get(string userCode, CancellationToken cancellationToken = default)
        {
            using var session = _sessionFunc();
            var request = await session.Query<DeviceAuthorizationData>()
                .Where(x => x.Response.UserCode == userCode)
                .Select(x => x.Response)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            return request switch
            {
                null => new ErrorDetails
                {
                    Detail = ErrorMessages.NotFound,
                    Title = ErrorMessages.NotFound,
                    Status = HttpStatusCode.NotFound
                },
                _ => request!
            };
        }

        /// <inheritdoc />
        public async Task<Option<DeviceAuthorizationData>> Get(string clientId, string deviceCode, CancellationToken cancellationToken = default)
        {
            using var session = _sessionFunc();
            var request = await session.Query<DeviceAuthorizationData>()
                .Where(x => x.ClientId == clientId && x.DeviceCode == deviceCode)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            return request switch
            {
                null => new ErrorDetails
                {
                    Detail = ErrorMessages.NotFound,
                    Title = ErrorMessages.NotFound,
                    Status = HttpStatusCode.NotFound
                },
                _ => request!
            };
        }

        /// <inheritdoc />
        public async Task<Option> Approve(string userCode, CancellationToken cancellationToken = default)
        {
            using var session = _sessionFunc();
            var data = await session.Query<DeviceAuthorizationData>().Where(x => x.Response.UserCode == userCode)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (data == null)
            {
                return new ErrorDetails
                {
                    Title = ErrorCodes.InvalidRequest,
                    Detail = "Not found",
                    Status = HttpStatusCode.NotFound
                };
            }

            data.Approved = true;
            session.Store(data);
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new Option.Success();
        }

        /// <inheritdoc />
        public async Task<Option> Save(DeviceAuthorizationData request, CancellationToken cancellationToken = default)
        {
            using var session = _sessionFunc();
            session.Store(request);
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new Option.Success();
        }

        /// <inheritdoc />
        public async Task<Option> Remove(DeviceAuthorizationData authRequest, CancellationToken cancellationToken)
        {
            using var session = _sessionFunc();
            session.Delete<DeviceAuthorizationData>(authRequest.DeviceCode);
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new Option.Success();
        }
    }
}