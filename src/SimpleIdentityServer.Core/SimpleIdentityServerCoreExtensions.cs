﻿// Copyright © 2015 Habart Thierry, © 2018 Jacob Reimers
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace SimpleIdentityServer.Core
{
    using Microsoft.Extensions.DependencyInjection;
    using SimpleIdentityServer.Core.Api.Authorization;
    using SimpleIdentityServer.Core.Api.Authorization.Actions;
    using SimpleIdentityServer.Core.Api.Authorization.Common;
    using SimpleIdentityServer.Core.Api.Discovery;
    using SimpleIdentityServer.Core.Api.Introspection;
    using SimpleIdentityServer.Core.Api.Introspection.Actions;
    using SimpleIdentityServer.Core.Api.Jwks;
    using SimpleIdentityServer.Core.Api.Jwks.Actions;
    using SimpleIdentityServer.Core.Api.Profile;
    using SimpleIdentityServer.Core.Api.Profile.Actions;
    using SimpleIdentityServer.Core.Api.Token;
    using SimpleIdentityServer.Core.Api.Token.Actions;
    using SimpleIdentityServer.Core.Api.UserInfo;
    using SimpleIdentityServer.Core.Api.UserInfo.Actions;
    using SimpleIdentityServer.Core.Authenticate;
    using SimpleIdentityServer.Core.Common;
    using SimpleIdentityServer.Core.Factories;
    using SimpleIdentityServer.Core.Helpers;
    using SimpleIdentityServer.Core.Jwt.Converter;
    using SimpleIdentityServer.Core.JwtToken;
    using SimpleIdentityServer.Core.Protector;
    using SimpleIdentityServer.Core.Repositories;
    using SimpleIdentityServer.Core.Services;
    using SimpleIdentityServer.Core.Translation;
    using SimpleIdentityServer.Core.Validators;
    using SimpleIdentityServer.Core.WebSite.Authenticate;
    using SimpleIdentityServer.Core.WebSite.Authenticate.Actions;
    using SimpleIdentityServer.Core.WebSite.Authenticate.Common;
    using SimpleIdentityServer.Core.WebSite.Consent;
    using SimpleIdentityServer.Core.WebSite.Consent.Actions;
    using SimpleIdentityServer.Core.WebSite.User.Actions;
    using System;
    using System.Collections.Generic;
    using Shared;
    using Shared.Models;
    using Shared.Repositories;
    using System.Net.Http;

    public static class SimpleIdentityServerCoreExtensions
    {
        public static IServiceCollection AddSimpleIdentityServerCore(
            this IServiceCollection serviceCollection,
            OAuthConfigurationOptions configurationOptions = null,
            IReadOnlyCollection<ClaimAggregate> claims = null,
            IReadOnlyCollection<Client> clients = null,
            IReadOnlyCollection<Consent> consents = null,
            IReadOnlyCollection<JsonWebKey> jsonWebKeys = null,
            IReadOnlyCollection<ResourceOwnerProfile> profiles = null,
            IReadOnlyCollection<ResourceOwner> resourceOwners = null,
            IReadOnlyCollection<Scope> scopes = null,
            IReadOnlyCollection<Shared.Models.Translation> translations = null)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.AddTransient<IGrantedTokenGeneratorHelper, GrantedTokenGeneratorHelper>();
            serviceCollection.AddTransient<IConsentHelper, ConsentHelper>();
            serviceCollection.AddTransient<IClientHelper, ClientHelper>();
            serviceCollection.AddTransient<IAuthorizationFlowHelper, AuthorizationFlowHelper>();
            serviceCollection.AddTransient<IClientCredentialsGrantTypeParameterValidator, ClientCredentialsGrantTypeParameterValidator>();
            serviceCollection.AddTransient<IClientValidator, ClientValidator>();
            serviceCollection.AddTransient<IScopeValidator, ScopeValidator>();
            serviceCollection.AddTransient<IGrantedTokenValidator, GrantedTokenValidator>();
            serviceCollection.AddTransient<IAuthorizationCodeGrantTypeParameterAuthEdpValidator, AuthorizationCodeGrantTypeParameterAuthEdpValidator>();
            serviceCollection.AddTransient<ICompressor, Compressor>();
            serviceCollection.AddTransient<IParameterParserHelper, ParameterParserHelper>();
            serviceCollection.AddTransient<IActionResultFactory, ActionResultFactory>();
            serviceCollection.AddTransient<IAuthorizationActions, AuthorizationActions>();
            serviceCollection.AddTransient<IGetAuthorizationCodeOperation, GetAuthorizationCodeOperation>();
            serviceCollection.AddTransient<IGetTokenViaImplicitWorkflowOperation, GetTokenViaImplicitWorkflowOperation>();
            serviceCollection.AddTransient<IUserInfoActions, UserInfoActions>();
            serviceCollection.AddTransient<IGetJwsPayload, GetJwsPayload>();
            serviceCollection.AddTransient<ITokenActions, TokenActions>();
            serviceCollection.AddTransient<IGetTokenByResourceOwnerCredentialsGrantTypeAction, GetTokenByResourceOwnerCredentialsGrantTypeAction>();
            serviceCollection.AddTransient<IGetTokenByAuthorizationCodeGrantTypeAction, GetTokenByAuthorizationCodeGrantTypeAction>();
            serviceCollection.AddTransient<IGetAuthorizationCodeAndTokenViaHybridWorkflowOperation, GetAuthorizationCodeAndTokenViaHybridWorkflowOperation>();
            serviceCollection.AddTransient<IConsentActions, ConsentActions>();
            serviceCollection.AddTransient<IConfirmConsentAction, ConfirmConsentAction>();
            serviceCollection.AddTransient<IDisplayConsentAction, DisplayConsentAction>();
            serviceCollection.AddTransient<IJwksActions, JwksActions>();
            serviceCollection.AddTransient<IRotateJsonWebKeysOperation, RotateJsonWebKeysOperation>();
            serviceCollection.AddTransient<IGetSetOfPublicKeysUsedToValidateJwsAction, GetSetOfPublicKeysUsedToValidateJwsAction>();
            serviceCollection.AddTransient<IJsonWebKeyEnricher, JsonWebKeyEnricher>();
            serviceCollection.AddTransient<IGetSetOfPublicKeysUsedByTheClientToEncryptJwsTokenAction, GetSetOfPublicKeysUsedByTheClientToEncryptJwsTokenAction>();
            serviceCollection.AddTransient<IAuthenticateActions, AuthenticateActions>();
            serviceCollection
                .AddTransient<IAuthenticateResourceOwnerOpenIdAction, AuthenticateResourceOwnerOpenIdAction>();
            serviceCollection.AddTransient<ILocalOpenIdUserAuthenticationAction, LocalOpenIdUserAuthenticationAction>();
            serviceCollection.AddTransient<IAuthenticateHelper, AuthenticateHelper>();
            serviceCollection.AddTransient<IDiscoveryActions, DiscoveryActions>();
            serviceCollection.AddTransient<IProcessAuthorizationRequest, ProcessAuthorizationRequest>();
            serviceCollection.AddTransient<IJwtGenerator, JwtGenerator>();
            serviceCollection.AddTransient<IJwtParser, JwtParser>();
            serviceCollection.AddTransient<IGenerateAuthorizationResponse, GenerateAuthorizationResponse>();
            serviceCollection.AddTransient<IAuthenticateClient, AuthenticateClient>();
            serviceCollection.AddTransient<IClientSecretBasicAuthentication, ClientSecretBasicAuthentication>();
            serviceCollection.AddTransient<IClientSecretPostAuthentication, ClientSecretPostAuthentication>();
            serviceCollection.AddTransient<IClientAssertionAuthentication, ClientAssertionAuthentication>();
            serviceCollection.AddTransient<IClientTlsAuthentication, ClientTlsAuthentication>();
            serviceCollection
                .AddTransient<IGetTokenByRefreshTokenGrantTypeAction, GetTokenByRefreshTokenGrantTypeAction>();
            serviceCollection.AddTransient<ITranslationManager, TranslationManager>();
            serviceCollection.AddTransient<IGrantedTokenHelper, GrantedTokenHelper>();

            serviceCollection.AddTransient<IIntrospectionActions, IntrospectionActions>();
            serviceCollection.AddTransient<IPostIntrospectionAction, PostIntrospectionAction>();
            serviceCollection.AddTransient<IIntrospectionParameterValidator, IntrospectionParameterValidator>();
            serviceCollection.AddTransient<IJsonWebKeyConverter, JsonWebKeyConverter>();
            serviceCollection.AddTransient<IGetConsentsOperation, GetConsentsOperation>();
            serviceCollection.AddTransient<IRemoveConsentOperation, RemoveConsentOperation>();
            serviceCollection.AddTransient<IRevokeTokenAction, RevokeTokenAction>();
            serviceCollection.AddTransient<IGetUserOperation, GetUserOperation>();
            serviceCollection.AddTransient<IUpdateUserCredentialsOperation, UpdateUserCredentialsOperation>();
            serviceCollection.AddTransient<IUpdateUserClaimsOperation, UpdateUserClaimsOperation>();
            serviceCollection.AddTransient<IAddUserOperation, AddUserOperation>();
            serviceCollection.AddTransient<IGenerateAndSendCodeAction, GenerateAndSendCodeAction>();
            serviceCollection.AddTransient<IValidateConfirmationCodeAction, ValidateConfirmationCodeAction>();
            serviceCollection.AddTransient<IRemoveConfirmationCodeAction, RemoveConfirmationCodeAction>();
            serviceCollection.AddTransient<ITwoFactorAuthenticationHandler, TwoFactorAuthenticationHandler>();
            serviceCollection.AddTransient<IPayloadSerializer, PayloadSerializer>();
            serviceCollection.AddTransient<IProfileActions, ProfileActions>();
            serviceCollection.AddTransient<ILinkProfileAction, LinkProfileAction>();
            serviceCollection.AddTransient<IUnlinkProfileAction, UnlinkProfileAction>();
            serviceCollection.AddTransient<IGetUserProfilesAction, GetUserProfilesAction>();
            serviceCollection.AddTransient<IGetResourceOwnerClaimsAction, GetResourceOwnerClaimsAction>();
            serviceCollection.AddTransient<IUpdateUserTwoFactorAuthenticatorOperation, UpdateUserTwoFactorAuthenticatorOperation>();
            serviceCollection.AddTransient<IResourceOwnerAuthenticateHelper, ResourceOwnerAuthenticateHelper>();
            serviceCollection.AddTransient<IAmrHelper, AmrHelper>();
            serviceCollection.AddTransient<IRevokeTokenParameterValidator, RevokeTokenParameterValidator>();
            serviceCollection.AddSingleton(configurationOptions ?? new OAuthConfigurationOptions());
            serviceCollection.AddSingleton<IClaimRepository>(new DefaultClaimRepository(claims));

            serviceCollection.AddSingleton(sp => new DefaultClientRepository(clients, sp.GetService<HttpClient>(), sp.GetService<IScopeStore>()));
            serviceCollection.AddSingleton(typeof(IClientStore), sp => sp.GetService<DefaultClientRepository>());
            serviceCollection.AddSingleton(typeof(IClientRepository), sp => sp.GetService<DefaultClientRepository>());
            serviceCollection.AddSingleton<IConsentRepository>(new DefaultConsentRepository(consents));
            serviceCollection.AddSingleton<IJsonWebKeyRepository>(new DefaultJsonWebKeyRepository(jsonWebKeys));
            serviceCollection.AddSingleton<IProfileRepository>(new DefaultProfileRepository(profiles));
            serviceCollection.AddSingleton<IResourceOwnerRepository>(
                new DefaultResourceOwnerRepository(resourceOwners));

            serviceCollection.AddSingleton(new DefaultScopeRepository(scopes));
            serviceCollection.AddSingleton<IScopeRepository>(sp => sp.GetService<DefaultScopeRepository>());
            serviceCollection.AddSingleton<IScopeStore>(sp => sp.GetService<DefaultScopeRepository>());
            serviceCollection.AddSingleton<ITranslationRepository>(new DefaultTranslationRepository(translations));
            return serviceCollection;
        }
    }
}
