﻿namespace SimpleAuth.Stores.Redis.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using global::Marten;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Npgsql;
    using SimpleAuth.Extensions;
    using SimpleAuth.Repositories;
    using SimpleAuth.Shared.Repositories;
    using SimpleAuth.Stores.Marten;
    using StackExchange.Redis;

    internal class ServerStartup
    {
        private const string DefaultSchema = CookieAuthenticationDefaults.AuthenticationScheme;
        private readonly SimpleAuthOptions _martenOptions;
        private readonly SharedContext _context;
        private readonly string _connectionString;
        private readonly string _schemaName;

        public ServerStartup(SharedContext context, string connectionString)
        {
            _martenOptions = new SimpleAuthOptions
            {
                AuthorizationCodes =
                    sp => new RedisAuthorizationCodeStore(
                        sp.GetRequiredService<IDatabaseAsync>(),
                        _martenOptions.AuthorizationCodeValidityPeriod),
                Clients = sp => new MartenClientStore(
                    sp.GetService<Func<IDocumentSession>>(),
                    sp.GetService<IScopeStore>(),
                    context.Client,
                    JsonConvert.DeserializeObject<Uri[]>),
                ConfirmationCodes =
                    sp => new RedisConfirmationCodeStore(
                        sp.GetRequiredService<IDatabaseAsync>(),
                        _martenOptions.RptLifeTime),
                Consents = sp => new RedisConsentStore(sp.GetRequiredService<IDatabaseAsync>()),
                JsonWebKeys = sp =>
                {
                    var keyset = new[] { context.SignatureKey, context.EncryptionKey }.ToJwks();
                    return new InMemoryJwksRepository(keyset, keyset);
                },
                Scopes = sp => new MartenScopeRepository(sp.GetService<Func<IDocumentSession>>()),
                Users = sp => new MartenResourceOwnerStore(sp.GetService<Func<IDocumentSession>>()),
                Tickets =
                    sp => new RedisTicketStore(sp.GetRequiredService<IDatabaseAsync>(), _martenOptions.TicketLifeTime),
                Tokens = sp => new RedisTokenStore(
                    sp.GetRequiredService<IDatabaseAsync>(),
                    sp.GetRequiredService<IJwksStore>())
            };
            _context = context;
            _connectionString = connectionString;
            var builder = new NpgsqlConnectionStringBuilder { ConnectionString = _connectionString };
            _schemaName = builder.SearchPath ?? "public";
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(ConnectionMultiplexer.Connect("localhost"));
            services.AddTransient<IDatabaseAsync>(sp => sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
            services.AddSingleton<IDocumentStore>(
                provider => new DocumentStore(
                    new SimpleAuthMartenOptions(
                        _connectionString,
                        new MartenLoggerFacade(provider.GetRequiredService<ILogger<MartenLoggerFacade>>()),
                        _schemaName)));
            services.AddTransient<Func<IDocumentSession>>(
                sp =>
                {
                    var store = sp.GetService<IDocumentStore>();
                    return () => store.LightweightSession();
                });
            // 1. Add the dependencies needed to enable CORS
            services.AddCors(
                options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
            // 2. Configure server
            services.AddSimpleAuth(_martenOptions, new[] { DefaultSchema, JwtBearerDefaults.AuthenticationScheme });
            services.AddLogging().AddAccountFilter().AddSingleton(sp => _context.Client);
            services.AddAuthentication(
                    cfg =>
                    {
                        cfg.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                        cfg.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                        cfg.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        cfg.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                .AddCookie(DefaultSchema)
                .AddJwtBearer(
                    JwtBearerDefaults.AuthenticationScheme,
                    cfg =>
                    {
                        cfg.Events = new JwtBearerEvents { OnAuthenticationFailed = c => Task.CompletedTask };
                        cfg.RequireHttpsMetadata = false;
                        cfg.TokenValidationParameters = new NoOpTokenValidationParameters(_context);
                    });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseSimpleAuthMvc();
        }
    }
}
