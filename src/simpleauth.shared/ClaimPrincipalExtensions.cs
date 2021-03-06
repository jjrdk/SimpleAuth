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

namespace SimpleAuth.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using Newtonsoft.Json;
    using SimpleAuth.Shared.Models;

    /// <summary>
    /// Defines the ClaimPrincipal extensions.
    /// </summary>
    public static class ClaimPrincipalExtensions
    {
        /// <summary>
        /// Tries to get the ticket lines from the current user claims.
        /// </summary>
        /// <param name="identity">The user as a <see cref="ClaimsIdentity"/> instance.</param>
        /// <param name="tickets">The found array of <see cref="TicketLine"/>. If none are found, then returns an empty array.
        /// If no user is found then returns <c>null</c>.</param>
        /// <returns><c>true</c> if any tickets are found, otherwise <c>false</c>.</returns>
        public static bool TryGetUmaTickets(this ClaimsIdentity identity, out Permission[] tickets)
        {
            Permission[] t = Array.Empty<Permission>();
            var result = identity?.Claims.TryGetUmaTickets(out t);
            tickets = t;
            return result == true;
        }

        /// <summary>
        /// Tries to get the ticket lines from the current user claims.
        /// </summary>
        /// <param name="claims">The user claims.</param>
        /// <param name="tickets">The found array of <see cref="TicketLine"/>. If none are found, then returns an empty array.
        /// If no user is found then returns <c>null</c>.</param>
        /// <returns><c>true</c> if any tickets are found, otherwise <c>false</c>.</returns>
        public static bool TryGetUmaTickets(this IEnumerable<Claim> claims, out Permission[] tickets)
        {
            tickets = Array.Empty<Permission>();

            try
            {
                tickets = claims.Where(c => c.Type == "permissions")
                    .SelectMany(
                        c => c.Value.StartsWith("[")
                            ? JsonConvert.DeserializeObject<Permission[]>(c.Value)!
                            : new[] { JsonConvert.DeserializeObject<Permission>(c.Value)! })
                    .ToArray();
                return tickets.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns if the user is authenticated
        /// </summary>
        /// <param name="principal">The user principal</param>
        /// <returns>The user is authenticated</returns>
        public static bool IsAuthenticated(this ClaimsPrincipal? principal)
        {
            return principal?.Identity?.IsAuthenticated == true;
        }

        /// <summary>
        /// Returns the subject from an authenticated user
        /// Otherwise returns null.
        /// </summary>
        /// <param name="principal">The user principal</param>
        /// <returns>User's subject</returns>
        public static string? GetSubject(this ClaimsPrincipal? principal)
        {
            var claim = principal?.FindFirst(OpenIdClaimTypes.Subject)
                ?? principal?.FindFirst(ClaimTypes.NameIdentifier);
            return claim?.Value;
        }

        /// <summary>
        /// Gets the client application id claim value.
        /// </summary>
        /// <param name="principal">The user principal.</param>
        /// <returns>the user's client.</returns>
        public static string? GetClientId(this ClaimsPrincipal? principal)
        {
            if (principal?.Identity == null || !principal.Identity.IsAuthenticated)
            {
                return string.Empty;
            }

            var claim = principal.Claims.FirstOrDefault(c => c.Type == StandardClaimNames.Azp);
            return claim == null ? string.Empty : claim.Value;
        }

        /// <summary>
        /// Gets the name of the authenticated user.
        /// </summary>
        /// <param name="principal">The user principal.</param>
        /// <returns>The user's name.</returns>
        public static string? GetName(this ClaimsPrincipal? principal)
        {
            return GetClaimValue(principal, OpenIdClaimTypes.Name)
                   ?? GetClaimValue(principal, StandardClaimNames.Subject)
                   ?? GetClaimValue(principal, ClaimTypes.Name)
                   ?? GetClaimValue(principal, ClaimTypes.NameIdentifier);
        }

        /// <summary>
        /// Gets the name of the authenticated user.
        /// </summary>
        /// <param name="principal">The user principal.</param>
        /// <returns>The user's name.</returns>
        public static string? GetEmail(this ClaimsPrincipal? principal)
        {
            return GetClaimValue(principal, OpenIdClaimTypes.Email)
                   ?? GetClaimValue(principal, ClaimTypes.Email);
        }

        private static string? GetClaimValue(ClaimsPrincipal? principal, string claimName)
        {
            var claim = principal?.FindFirst(claimName);

            return claim?.Value;
        }
    }
}
