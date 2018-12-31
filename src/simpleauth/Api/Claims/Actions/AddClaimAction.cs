﻿namespace SimpleAuth.Api.Claims.Actions
{
    using System;
    using System.Threading.Tasks;
    using Errors;
    using Exceptions;
    using Shared.Parameters;
    using Shared.Repositories;

    public class AddClaimAction : IAddClaimAction
    {
        private readonly IClaimRepository _claimRepository;

        public AddClaimAction(IClaimRepository claimRepository)
        {
            _claimRepository = claimRepository;
        }

        public async Task<bool> Execute(AddClaimParameter request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                throw new ArgumentNullException(nameof(request.Code));
            }

            if (request.IsIdentifier)
            {
                throw new SimpleAuthException(ErrorCodes.InvalidRequestCode, ErrorDescriptions.CannotInsertClaimIdentifier);
            }

            var claim = await _claimRepository.GetAsync(request.Code).ConfigureAwait(false);
            if (claim != null)
            {
                throw new SimpleAuthException(ErrorCodes.InvalidRequestCode, ErrorDescriptions.ClaimExists);
            }

            return await _claimRepository.InsertAsync(request).ConfigureAwait(false);
        }
    }
}
