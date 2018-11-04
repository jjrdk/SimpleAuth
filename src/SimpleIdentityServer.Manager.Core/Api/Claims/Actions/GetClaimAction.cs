﻿using SimpleIdentityServer.Manager.Core.Errors;
using SimpleIdentityServer.Manager.Core.Exceptions;
using System;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Manager.Core.Api.Claims.Actions
{
    using Shared.Models;
    using Shared.Repositories;

    public interface IGetClaimAction
    {
        Task<ClaimAggregate> Execute(string claimCode);
    }

    internal sealed class GetClaimAction : IGetClaimAction
    {
        private readonly IClaimRepository _claimRepository;

        public GetClaimAction(IClaimRepository claimRepository)
        {
            _claimRepository = claimRepository;
        }

        public async Task<ClaimAggregate> Execute(string claimCode)
        {
            if (string.IsNullOrWhiteSpace(claimCode))
            {
                throw new ArgumentNullException(nameof(claimCode));
            }

            var claim = await _claimRepository.GetAsync(claimCode).ConfigureAwait(false);
            if (claim == null)
            {
                throw new IdentityServerManagerException(ErrorCodes.InvalidRequestCode, ErrorDescriptions.ClaimDoesntExist);
            }

            return await _claimRepository.GetAsync(claimCode).ConfigureAwait(false);
        }
    }
}
