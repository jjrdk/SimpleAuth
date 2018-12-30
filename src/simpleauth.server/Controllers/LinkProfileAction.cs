﻿namespace SimpleAuth.Server.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Exceptions;
    using Shared.Models;
    using Shared.Repositories;

    internal sealed class LinkProfileAction
    {
        private readonly IResourceOwnerRepository _resourceOwnerRepository;
        private readonly IProfileRepository _profileRepository;

        public LinkProfileAction(IResourceOwnerRepository resourceOwnerRepository, IProfileRepository profileRepository)
        {
            _resourceOwnerRepository = resourceOwnerRepository;
            _profileRepository = profileRepository;
        }

        public async Task<bool> Execute(string localSubject, string externalSubject, string issuer, bool force = false)
        {
            var resourceOwner = await _resourceOwnerRepository.Get(localSubject).ConfigureAwait(false);
            if (resourceOwner == null)
            {
                throw new IdentityServerException(
                    Errors.ErrorCodes.InternalError, 
                    string.Format(Errors.ErrorDescriptions.TheResourceOwnerDoesntExist, localSubject));
            }

            var profile = await _profileRepository.Get(externalSubject).ConfigureAwait(false);
            if (profile != null && profile.ResourceOwnerId != localSubject)
            {
                if (!force)
                {
                    throw new ProfileAssignedAnotherAccountException();
                }

                await _profileRepository.Remove(new[] { externalSubject }).ConfigureAwait(false);
                if (profile.ResourceOwnerId == profile.Subject)
                {
                    await _resourceOwnerRepository.Delete(profile.ResourceOwnerId).ConfigureAwait(false);
                }

                profile = null;
            }

            if (profile != null)
            {
                throw new IdentityServerException(Errors.ErrorCodes.InternalError, Errors.ErrorDescriptions.TheProfileAlreadyLinked);
            }

            return await _profileRepository.Add(new[]
            {
                new ResourceOwnerProfile
                {
                    ResourceOwnerId = localSubject,
                    Subject = externalSubject,
                    Issuer = issuer,
                    CreateDateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow
                }
            }).ConfigureAwait(false);
        }
    }
}
