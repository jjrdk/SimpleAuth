﻿namespace SimpleAuth.Api.Profile.Actions
{
    using Shared.Models;
    using Shared.Repositories;
    using System;
    using System.Threading.Tasks;

    internal sealed class GetResourceOwnerClaimsAction
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IResourceOwnerRepository _resourceOwnerRepository;

        public GetResourceOwnerClaimsAction(IProfileRepository profileRepository, IResourceOwnerRepository resourceOwnerRepository)
        {
            _profileRepository = profileRepository;
            _resourceOwnerRepository = resourceOwnerRepository;
        }

        public async Task<ResourceOwner> Execute(string externalSubject)
        {
            if (string.IsNullOrWhiteSpace(externalSubject))
            {
                throw new ArgumentNullException(nameof(externalSubject));
            }

            var profile = await _profileRepository.Get(externalSubject).ConfigureAwait(false);
            if (profile == null)
            {
                return null;
            }

            return await _resourceOwnerRepository.Get(profile.ResourceOwnerId).ConfigureAwait(false);
        }
    }
}
