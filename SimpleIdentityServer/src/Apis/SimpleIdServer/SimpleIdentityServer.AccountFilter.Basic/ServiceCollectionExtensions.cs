﻿using Microsoft.Extensions.DependencyInjection;
using SimpleIdentityServer.AccountFilter.Basic.Aggregates;
using SimpleIdentityServer.AccountFilter.Basic.Repositories;
using System;
using System.Collections.Generic;

namespace SimpleIdentityServer.AccountFilter.Basic
{
    using Core.Common;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAccountFilter(this IServiceCollection services, List<FilterAggregate> filters = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddTransient<IAccountFilter, AccountFilter>();
            services.AddSingleton<IFilterRepository>(new DefaultFilterRepository(filters));
            return services;
        }
    }
}
