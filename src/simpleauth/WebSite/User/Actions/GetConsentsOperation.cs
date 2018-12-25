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

namespace SimpleAuth.WebSite.User.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Extensions;
    using Shared.Models;
    using Shared.Repositories;

    internal class GetConsentsOperation : IGetConsentsOperation
    {
        private readonly IConsentRepository _consentRepository;

        public GetConsentsOperation(IConsentRepository consentRepository)
        {
            _consentRepository = consentRepository;
        }
        
        public async Task<IEnumerable<Consent>> Execute(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal?.Identity == null)
            {
                throw new ArgumentNullException(nameof(claimsPrincipal));
            }

            var subject = claimsPrincipal.GetSubject();
            return await _consentRepository.GetConsentsForGivenUserAsync(subject).ConfigureAwait(false);
        }
    }
}
