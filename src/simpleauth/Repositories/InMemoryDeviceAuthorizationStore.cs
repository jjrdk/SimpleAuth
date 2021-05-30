﻿namespace SimpleAuth.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Repositories;
    using SimpleAuth.Shared.Requests;
    using SimpleAuth.Shared.Responses;

    internal sealed class InMemoryDeviceAuthorizationStore : IDeviceAuthorizationStore
    {
        private readonly List<DeviceAuthorizationData> _requests = new();

        /// <inheritdoc />
        public Task<Option<DeviceAuthorizationResponse>> Get(string userCode, CancellationToken cancellationToken = default)
        {
            var result = _requests.First(x => x.Response.UserCode == userCode);

            return Task.FromResult<Option<DeviceAuthorizationResponse>>(result.Response);
        }

        /// <inheritdoc />
        public Task<Option<DeviceAuthorizationData>> Get(string clientId, string deviceCode, CancellationToken cancellationToken = default)
        {
            var result = _requests.First(x => x.ClientId == clientId && x.DeviceCode == deviceCode);

            return Task.FromResult<Option<DeviceAuthorizationData>>(result);
        }

        /// <inheritdoc />
        public Task<Option> Approve(string userCode, CancellationToken cancellationToken = default)
        {
            var result = _requests.First(x => x.Response.UserCode == userCode);
            result.Approved = true;

            return Task.FromResult<Option>(new Option.Success());
        }

        /// <inheritdoc />
        public Task<Option> Save(DeviceAuthorizationData request, CancellationToken cancellationToken = default)
        {
            _requests.Add(request);
            return Task.FromResult<Option>(new Option.Success());
        }
    }
}