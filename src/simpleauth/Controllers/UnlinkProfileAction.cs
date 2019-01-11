﻿namespace SimpleAuth.Server.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Exceptions;
    using Shared.Repositories;

    internal sealed class UnlinkProfileAction
    {
        private readonly IResourceOwnerRepository _resourceOwnerRepository;
        private readonly IProfileRepository _profileRepository;

        public UnlinkProfileAction(IResourceOwnerRepository resourceOwnerRepository, IProfileRepository profileRepository)
        {
            _resourceOwnerRepository = resourceOwnerRepository;
            _profileRepository = profileRepository;
        }

        public async Task<bool> Execute(string localSubject, string externalSubject)
        {
            if (string.IsNullOrWhiteSpace(localSubject))
            {
                throw new ArgumentNullException(nameof(localSubject));
            }

            if (string.IsNullOrWhiteSpace(externalSubject))
            {
                throw new ArgumentNullException(nameof(externalSubject));
            }

            var resourceOwner = await _resourceOwnerRepository.Get(localSubject).ConfigureAwait(false);
            if (resourceOwner == null)
            {
                throw new SimpleAuthException(
                    Errors.ErrorCodes.InternalError,
                    string.Format(Errors.ErrorDescriptions.TheResourceOwnerDoesntExist, localSubject));
            }

            var profile = await _profileRepository.Get(externalSubject).ConfigureAwait(false);
            if (profile == null || profile.ResourceOwnerId != localSubject)
            {
                throw new SimpleAuthException(Errors.ErrorCodes.InternalError, Errors.ErrorDescriptions.NotAuthorizedToRemoveTheProfile);
            }

            if (profile.Subject == localSubject)
            {
                throw new SimpleAuthException(Errors.ErrorCodes.InternalError, Errors.ErrorDescriptions.TheExternalAccountAccountCannotBeUnlinked);
            }

            return await _profileRepository.Remove(new[] { externalSubject }).ConfigureAwait(false);
        }
    }
}
