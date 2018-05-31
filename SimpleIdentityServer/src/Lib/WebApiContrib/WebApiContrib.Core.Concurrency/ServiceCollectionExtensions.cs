﻿using Microsoft.Extensions.DependencyInjection;
using System;
using WebApiContrib.Core.Storage;

namespace WebApiContrib.Core.Concurrency
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConcurrency(
            this IServiceCollection serviceCollection,
            Action<StorageOptionsBuilder> callback)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var builder = new StorageOptionsBuilder(serviceCollection);
            callback(builder);
            serviceCollection.AddSingleton(builder.StorageOptions);
            serviceCollection.AddConcurrency();
            return serviceCollection;
        }

        public static IServiceCollection AddConcurrency(this IServiceCollection serviceCollection)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.AddTransient<IConcurrencyManager, ConcurrencyManager>();
            serviceCollection.AddTransient<IRepresentationManager, RepresentationManager>();
            return serviceCollection;
        }

    }
}
