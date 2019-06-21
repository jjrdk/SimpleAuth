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
        public ResourceOwnerDeleted(string id, string subject, PostClaim[] claims, DateTime timestamp) : base(id, timestamp)
        {
            Subject = subject;
            Claims = claims;
        }

        public string Subject { get; }

        public PostClaim[] Claims { get; }
    }
}