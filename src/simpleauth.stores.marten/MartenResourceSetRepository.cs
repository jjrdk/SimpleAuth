﻿namespace SimpleAuth.Stores.Marten
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Marten;
    using global::Marten.Pagination;

    using SimpleAuth.Shared.DTOs;
    using SimpleAuth.Shared.Models;
    using SimpleAuth.Shared.Repositories;

    /// <summary>
    /// Defines the marten based resource set repository.
    /// </summary>
    /// <seealso cref="SimpleAuth.Shared.Repositories.IResourceSetRepository" />
    public class MartenResourceSetRepository : IResourceSetRepository
    {
        private readonly Func<IDocumentSession> _sessionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MartenScopeRepository"/> class.
        /// </summary>
        /// <param name="sessionFactory">The session factory.</param>
        public MartenResourceSetRepository(Func<IDocumentSession> sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        /// <inheritdoc />
        public async Task<PagedResult<ResourceSetModel>> Search(
            SearchResourceSet parameter,
            CancellationToken cancellationToken)
        {
            using (var session = _sessionFactory())
            {
                parameter.StartIndex++;
                parameter.Ids = parameter.Ids ?? Array.Empty<string>();
                parameter.Names = parameter.Names ?? Array.Empty<string>();
                var results = await session.Query<ResourceSetModel>()
                    .Where(x => x.Name.IsOneOf(parameter.Ids) && x.Type.IsOneOf(parameter.Names))
                    .ToPagedListAsync(parameter.StartIndex, parameter.TotalResults, cancellationToken)
                    .ConfigureAwait(false);

                return new PagedResult<ResourceSetModel>
                {
                    Content = results.ToArray(),
                    StartIndex = parameter.StartIndex,
                    TotalResults = results.TotalItemCount
                };
            }
        }

        /// <inheritdoc />
        public async Task<bool> Add(ResourceSetModel resourceSet, CancellationToken cancellationToken)
        {
            using (var session = _sessionFactory())
            {
                session.Store(resourceSet);
                await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
        }

        /// <inheritdoc />
        public async Task<ResourceSetModel> Get(string id, CancellationToken cancellationToken)
        {
            using (var session = _sessionFactory())
            {
                var resourceSet = await session.LoadAsync<ResourceSetModel>(id, cancellationToken).ConfigureAwait(false);

                return resourceSet;
            }
        }

        /// <inheritdoc />
        public async Task<bool> Update(ResourceSetModel resourceSet, CancellationToken cancellationToken)
        {
            using (var session = _sessionFactory())
            {
                session.Update(resourceSet);
                await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
        }

        /// <inheritdoc />
        public async Task<ResourceSetModel[]> GetAll(string owner, CancellationToken cancellationToken)
        {
            using (var session = _sessionFactory())
            {
                var resourceSets = await session.Query<ResourceSetModel>()
                    .Where(x => x.Owner == owner)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return resourceSets.ToArray();
            }
        }

        /// <inheritdoc />
        public async Task<bool> Remove(string id, CancellationToken cancellationToken)
        {
            using (var session = _sessionFactory())
            {
                session.Delete<ResourceSetModel>(id);
                await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
        }

        /// <inheritdoc />
        public async Task<ResourceSetModel[]> Get(CancellationToken cancellationToken = default, params string[] ids)
        {
            using (var session = _sessionFactory())
            {
                var resourceSets =
                    await session.LoadManyAsync<ResourceSetModel>(cancellationToken, ids).ConfigureAwait(false);

                return resourceSets.ToArray();
            }
        }
    }
}
