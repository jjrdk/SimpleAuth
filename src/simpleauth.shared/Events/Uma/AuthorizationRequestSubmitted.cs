﻿namespace SimpleAuth.Shared.Events.Uma
{
    using System;
    using System.Security.Claims;

    /// <summary>
    /// Defines the authorization request submitted event.
    /// </summary>
    /// <seealso cref="Event" />
    public class AuthorizationRequestSubmitted : UmaTicketEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationRequestSubmitted"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="ticketId">The ticket identifier.</param>
        /// <param name="requester"></param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="clientId"></param>
        public AuthorizationRequestSubmitted(string id, string ticketId, string clientId, ClaimsPrincipal requester, DateTimeOffset timestamp)
            : base(id, ticketId, clientId, requester, timestamp)
        {
        }
    }
}