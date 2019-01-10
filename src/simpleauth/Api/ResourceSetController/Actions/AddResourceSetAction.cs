// Copyright � 2015 Habart Thierry, � 2018 Jacob Reimers
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

namespace SimpleAuth.Api.ResourceSetController.Actions
{
    using Errors;
    using Exceptions;
    using Parameters;
    using Repositories;
    using Shared;
    using Shared.Models;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    internal class AddResourceSetAction : IAddResourceSetAction
    {
        private readonly IResourceSetRepository _resourceSetRepository;

        public AddResourceSetAction(IResourceSetRepository resourceSetRepository)
        {
            _resourceSetRepository = resourceSetRepository;
        }

        public async Task<string> Execute(AddResouceSetParameter addResourceSetParameter)
        {
            if (addResourceSetParameter == null)
            {
                throw new ArgumentNullException(nameof(addResourceSetParameter));
            }

            var resourceSet = new ResourceSet
            {
                Id = Id.Create(),
                Name = addResourceSetParameter.Name,
                Uri = addResourceSetParameter.Uri,
                Type = addResourceSetParameter.Type,
                Scopes = addResourceSetParameter.Scopes,
                IconUri = addResourceSetParameter.IconUri
            };

            CheckResourceSetParameter(resourceSet);
            if (!await _resourceSetRepository.Insert(resourceSet).ConfigureAwait(false))
            {
                throw new SimpleAuthException(ErrorCodes.InternalError,
                    ErrorDescriptions.TheResourceSetCannotBeInserted);
            }

            return resourceSet.Id;
        }

        public void CheckResourceSetParameter(ResourceSet resourceSet)
        {
            if (resourceSet == null)
            {
                throw new ArgumentNullException(nameof(resourceSet));
            }

            if (string.IsNullOrWhiteSpace(resourceSet.Name))
            {
                throw new SimpleAuthException(ErrorCodes.InvalidRequestCode,
                    string.Format(ErrorDescriptions.TheParameterNeedsToBeSpecified, "name"));
            }

            if (resourceSet.Scopes == null ||
                !resourceSet.Scopes.Any())
            {
                throw new SimpleAuthException(ErrorCodes.InvalidRequestCode,
                    string.Format(ErrorDescriptions.TheParameterNeedsToBeSpecified, "scopes"));
            }

            if (!string.IsNullOrWhiteSpace(resourceSet.IconUri) &&
                !Uri.IsWellFormedUriString(resourceSet.IconUri, UriKind.Absolute))
            {
                throw new SimpleAuthException(ErrorCodes.InvalidRequestCode,
                    string.Format(ErrorDescriptions.TheUrlIsNotWellFormed, resourceSet.IconUri));
            }

            if (!string.IsNullOrWhiteSpace(resourceSet.Uri) &&
                !Uri.IsWellFormedUriString(resourceSet.Uri, UriKind.Absolute))
            {
                throw new SimpleAuthException(ErrorCodes.InvalidRequestCode,
                    string.Format(ErrorDescriptions.TheUrlIsNotWellFormed, resourceSet.Uri));
            }
        }
    }
}
