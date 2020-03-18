﻿namespace SimpleAuth.Stores.Marten
{
    using global::Marten;
    using Microsoft.IdentityModel.Tokens;
    using NpgsqlTypes;
    using SimpleAuth.Shared.Models;

    /// <summary>
    /// Defines the default marten registry for stored SimpleAuth types.
    /// </summary>
    /// <seealso cref="MartenRegistry" />
    public class SimpleAuthRegistry : MartenRegistry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleAuthRegistry"/> class.
        /// </summary>
        public SimpleAuthRegistry()
        {
            For<Scope>()
                .Identity(x => x.Name)
                .Duplicate(x => x.IsDisplayedInConsent, dbType: NpgsqlDbType.Boolean)
                .Duplicate(x => x.Type, "varchar(15)")
                .GinIndexJsonData();
            For<Filter>().Identity(x => x.Name).GinIndexJsonData();
            For<ResourceOwner>()
                .Identity(x => x.Subject)
                .Index(x => x.Claims)
                .Index(x => x.ExternalLogins)
                .GinIndexJsonData();
            For<Consent>()
                .Identity(x => x.Id)
                .Duplicate(x => x.ResourceOwner.Subject)
                .GinIndexJsonData();
            For<Client>()
                .Identity(x => x.ClientId)
                .Index(x => x.AllowedScopes)
                .Index(x => x.GrantTypes)
                .Duplicate(x => x.IdTokenEncryptedResponseAlg, "varchar(10)")
                .Index(x => x.ResponseTypes)
                .Index(x => x.Claims)
                .GinIndexJsonData();
            For<ResourceSet>()
                .Identity(x => x.Id)
                .Duplicate(x => x.Name, configure: idx => { idx.IsConcurrent = true; })
                .Duplicate(x => x.Type, configure: idx => { idx.IsConcurrent = true; })
                .GinIndexJsonData();
            For<Ticket>()
                .Identity(x => x.Id)
                .Duplicate(x => x.Created, configure: idx => { idx.IsConcurrent = true; })
                .Duplicate(x => x.Expires, configure: idx => { idx.IsConcurrent = true; })
                .Duplicate(x => x.IsAuthorizedByRo, configure: idx => { idx.IsConcurrent = true; }, dbType: NpgsqlDbType.Boolean)
                .Duplicate(x => x.ClientId, configure: idx => { idx.IsConcurrent = true; })
                .GinIndexJsonData();
            For<AuthorizationCode>()
                .Identity(x => x.Code)
                .Duplicate(x => x.ClientId)
                .GinIndexJsonData();
            For<ConfirmationCode>().Identity(x => x.Value).GinIndexJsonData();
            For<GrantedToken>()
                .Duplicate(x => x.Scope)
                .Duplicate(x => x.AccessToken)
                .Duplicate(x => x.ClientId)
                .Duplicate(x => x.CreateDateTime)
                .Duplicate(x => x.ExpiresIn, dbType: NpgsqlDbType.Integer)
                .Duplicate(x => x.IdToken)
                .Duplicate(x => x.ParentTokenId, dbType: NpgsqlDbType.Uuid)
                .Duplicate(x => x.RefreshToken)
                .Duplicate(x => x.TokenType, "character(10)")
                .GinIndexJsonData();
            For<JsonWebKey>()
                .Identity(x => x.Kid)
                .Duplicate(x => x.Alg, pgType: "character(20)")
                .Duplicate(x => x.Use, "character(3)")
                .Duplicate(x => x.HasPrivateKey, dbType: NpgsqlDbType.Boolean)
                .Index(x => x.KeyOps)
                .GinIndexJsonData();
        }
    }
}
