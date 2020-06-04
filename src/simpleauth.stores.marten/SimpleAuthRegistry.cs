﻿namespace SimpleAuth.Stores.Marten
{
    using global::Marten;
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
                .Duplicate(x => x.Subject)
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
                .Duplicate(x => x.Name)
                .Duplicate(x => x.Type)
                .GinIndexJsonData();
            For<Ticket>()
                .Identity(x => x.Id)
                .Duplicate(x => x.Created)
                .Duplicate(x => x.Expires)
                .Duplicate(x => x.IsAuthorizedByRo, dbType: NpgsqlDbType.Boolean)
                .GinIndexJsonData();
            For<AuthorizationCode>()
                .Identity(x => x.Code)
                .Duplicate(x => x.ClientId)
                .GinIndexJsonData();
            For<ConfirmationCode>().Identity(x => x.Value).GinIndexJsonData();
            For<GrantedToken>()
                .Identity(x => x.Id)
                .Duplicate(x => x.Scope)
                .Duplicate(x => x.AccessToken)
                .Duplicate(x => x.ClientId)
                .Duplicate(x => x.CreateDateTime)
                .Duplicate(x => x.ExpiresIn, dbType: NpgsqlDbType.Integer)
                .Duplicate(x => x.IdToken)
                .Duplicate(x => x.ParentTokenId)
                .Duplicate(x => x.RefreshToken)
                .Duplicate(x => x.TokenType, "character(10)")
                .GinIndexJsonData();
            For<JsonWebKeyContainer>()
                .Identity(x => x.Id)
                .Duplicate(x => x.Jwk.Alg, pgType: "character(20)")
                .Duplicate(x => x.Jwk.Use, "character(3)")
                .Duplicate(x => x.Jwk.HasPrivateKey, dbType: NpgsqlDbType.Boolean)
                .Index(x => x.Jwk.KeyOps)
                .GinIndexJsonData();
        }
    }
}
