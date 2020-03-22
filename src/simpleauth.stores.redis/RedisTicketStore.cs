﻿namespace SimpleAuth.Stores.Redis
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using SimpleAuth.Shared.Models;
    using SimpleAuth.Shared.Repositories;
    using StackExchange.Redis;

    /// <summary>
    /// Defines the Redis ticket store.
    /// </summary>
    public class RedisTicketStore : ITicketStore
    {
        private readonly IDatabaseAsync _database;
        private readonly TimeSpan _expiry;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisTicketStore"/> class.
        /// </summary>
        /// <param name="database">The underlying Redis store.</param>
        /// <param name="expiry">The default cache expiration.</param>
        public RedisTicketStore(IDatabaseAsync database, TimeSpan expiry = default)
        {
            _database = database;
            _expiry = expiry == default ? TimeSpan.FromMinutes(30) : expiry;
        }

        /// <inheritdoc />
        public Task<bool> Add(Ticket ticket, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(ticket);
            return _database.StringSetAsync(ticket.Id, json, _expiry);
        }

        /// <inheritdoc />
        public async Task<bool> ApproveAccess(string ticketId, CancellationToken cancellationToken = default)
        {
            var value = await _database.StringGetAsync(ticketId).ConfigureAwait(false);
            if (!value.HasValue)
            {
                return false;
            }

            var ticket = JsonConvert.DeserializeObject<Ticket>(value);
            if (ticket.IsAuthorizedByRo)
            {
                return false;
            }

            ticket.IsAuthorizedByRo = true;
            return await _database.StringSetAsync(ticket.Id, JsonConvert.SerializeObject(ticket), _expiry).ConfigureAwait(false);

        }

        /// <inheritdoc />
        public Task<bool> Remove(string ticketId, CancellationToken cancellationToken)
        {
            return _database.KeyDeleteAsync(ticketId);
        }

        /// <inheritdoc />
        public async Task<Ticket> Get(string ticketId, CancellationToken cancellationToken)
        {
            var ticket = await _database.StringGetAsync(ticketId).ConfigureAwait(false);
            return ticket.HasValue
                ? JsonConvert.DeserializeObject<Ticket>(ticket)
                : null;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<Ticket>> GetAll(string owner, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task Clean(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}