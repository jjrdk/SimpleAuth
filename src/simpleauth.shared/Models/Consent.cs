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

namespace SimpleAuth.Shared.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines the consent content.
    /// </summary>
    public class Consent
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public Client Client { get; set; }

        /// <summary>
        /// Gets or sets the resource owner.
        /// </summary>
        /// <value>
        /// The resource owner.
        /// </value>
        public ResourceOwner ResourceOwner { get; set; }

        /// <summary>
        /// Gets or sets the granted scopes.
        /// </summary>
        /// <value>
        /// The granted scopes.
        /// </value>
        public string[] GrantedScopes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the claims.
        /// </summary>
        /// <value>
        /// The claims.
        /// </value>
        public List<string> Claims { get; set; }
    }
}
