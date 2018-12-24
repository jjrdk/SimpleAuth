﻿using SimpleIdentityServer.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Core.Api.Profile.Actions
{
    using Shared.Models;
    using Shared.Parameters;
    using Shared.Repositories;

    internal sealed class GetUserProfilesAction : IGetUserProfilesAction
    {
        private readonly IResourceOwnerRepository _resourceOwnerRepository;
        private readonly IProfileRepository _profileRepository;

        public GetUserProfilesAction(IProfileRepository profileRepository,
            IResourceOwnerRepository resourceOwnerRepository)
        {
            _profileRepository = profileRepository;
            _resourceOwnerRepository = resourceOwnerRepository;
        }

        /// <summary>
        /// Get the profiles linked to the user account.
        /// </summary>
        /// <param name="subject"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ResourceOwnerProfile>> Execute(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentNullException(nameof(subject));
            }

            var resourceOwner = await _resourceOwnerRepository.Get(subject).ConfigureAwait(false);
            if (resourceOwner == null)
            {
                throw new IdentityServerException(
                    Errors.ErrorCodes.InternalError,
                    string.Format(Errors.ErrorDescriptions.TheResourceOwnerDoesntExist, subject));
            }

            return await _profileRepository.Search(new SearchProfileParameter
                {
                    ResourceOwnerIds = new[] {subject}
                })
                .ConfigureAwait(false);
        }
    }
}
