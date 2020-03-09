﻿namespace SimpleAuth.Shared.Events.Logging
{
    using System;
    using DTOs;

    /// <summary>
    /// Defines the resource owner added event.
    /// </summary>
    /// <seealso cref="Event" />
    public class ResourceOwnerDeleted : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceOwnerAdded"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="subject">The account subject</param>
        /// <param name="claims">The claims of the deleted user.</param>
        /// <param name="timestamp">The timestamp.</param>
        public ResourceOwnerDeleted(string id, string subject, ClaimData[] claims, DateTimeOffset timestamp) : base(id, timestamp)
        {
            Subject = subject;
            Claims = claims;
        }

        /// <summary>
        /// Gets the subject of the removed resource owner.
        /// </summary>
        public string Subject { get; }

        /// <summary>
        /// Gets the claims for the removed resource owner.
        /// </summary>
        public ClaimData[] Claims { get; }
    }
}