﻿namespace SimpleAuth.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Shared.Models;
    using Shared.Parameters;
    using Shared.Repositories;
    using Shared.Results;

    internal sealed class DefaultScopeRepository : IScopeRepository
    {
        public ICollection<Scope> _scopes;

        private readonly List<Scope> DEFAULT_SCOPES = new List<Scope>
        {
            new Scope
            {
                Name = "openid",
                IsExposed = true,
                IsOpenIdScope = true,
                IsDisplayedInConsent = true,
                Description = "access to the openid scope",
                Type = ScopeType.ProtectedApi,
                Claims = new List<string>()
            },
            new Scope
            {
                Name = "profile",
                IsExposed = true,
                IsOpenIdScope = true,
                Description = "Access to the profile",
                Claims = new List<string>
                {
                    JwtConstants.StandardResourceOwnerClaimNames.Name,
                    JwtConstants.StandardResourceOwnerClaimNames.FamilyName,
                    JwtConstants.StandardResourceOwnerClaimNames.GivenName,
                    JwtConstants.StandardResourceOwnerClaimNames.MiddleName,
                    JwtConstants.StandardResourceOwnerClaimNames.NickName,
                    JwtConstants.StandardResourceOwnerClaimNames.PreferredUserName,
                    JwtConstants.StandardResourceOwnerClaimNames.Profile,
                    JwtConstants.StandardResourceOwnerClaimNames.Picture,
                    JwtConstants.StandardResourceOwnerClaimNames.WebSite,
                    JwtConstants.StandardResourceOwnerClaimNames.Gender,
                    JwtConstants.StandardResourceOwnerClaimNames.BirthDate,
                    JwtConstants.StandardResourceOwnerClaimNames.ZoneInfo,
                    JwtConstants.StandardResourceOwnerClaimNames.Locale,
                    JwtConstants.StandardResourceOwnerClaimNames.UpdatedAt
                },
                Type = ScopeType.ResourceOwner,
                IsDisplayedInConsent = true
            },
            new Scope
            {
                Name = "scim",
                IsExposed = true,
                IsOpenIdScope = true,
                Description = "Access to the scim",
                Claims = new List<string>
                {
                    JwtConstants.StandardResourceOwnerClaimNames.ScimId,
                    JwtConstants.StandardResourceOwnerClaimNames.ScimLocation
                },
                Type = ScopeType.ResourceOwner,
                IsDisplayedInConsent = true
            },
            new Scope
            {
                Name = "email",
                IsExposed = true,
                IsOpenIdScope = true,
                IsDisplayedInConsent = true,
                Description = "Access to the email",
                Claims = new List<string>
                {
                    JwtConstants.StandardResourceOwnerClaimNames.Email,
                    JwtConstants.StandardResourceOwnerClaimNames.EmailVerified
                },
                Type = ScopeType.ResourceOwner
            },
            new Scope
            {
                Name = "address",
                IsExposed = true,
                IsOpenIdScope = true,
                IsDisplayedInConsent = true,
                Description = "Access to the address",
                Claims = new List<string>
                {
                    JwtConstants.StandardResourceOwnerClaimNames.Address
                },
                Type = ScopeType.ResourceOwner
            },
            new Scope
            {
                Name = "phone",
                IsExposed = true,
                IsOpenIdScope = true,
                IsDisplayedInConsent = true,
                Description = "Access to the phone",
                Claims = new List<string>
                {
                    JwtConstants.StandardResourceOwnerClaimNames.PhoneNumber,
                    JwtConstants.StandardResourceOwnerClaimNames.PhoneNumberVerified
                },
                Type = ScopeType.ResourceOwner
            },
            new Scope
            {
                Name = "role",
                IsExposed = true,
                IsOpenIdScope = false,
                IsDisplayedInConsent = true,
                Description = "Access to your roles",
                Claims = new List<string>
                {
                    JwtConstants.StandardResourceOwnerClaimNames.Role
                },
                Type = ScopeType.ResourceOwner
            },
            new Scope
            {
                Name = "register_client",
                IsExposed = false,
                IsOpenIdScope = false,
                IsDisplayedInConsent = true,
                Description = "Register a client",
                Type = ScopeType.ProtectedApi
            },
            new Scope
            {
                Name = "manage_profile",
                IsExposed = false,
                IsOpenIdScope = false,
                IsDisplayedInConsent = true,
                Description = "Manage the user's profiles",
                Type = ScopeType.ProtectedApi
            },
            new Scope
            {
                Name = "manage_account_filtering",
                IsExposed = false,
                IsOpenIdScope = false,
                IsDisplayedInConsent = true,
                Description = "Manage the account filtering",
                Type = ScopeType.ProtectedApi
            }
        };

        public DefaultScopeRepository(IReadOnlyCollection<Scope> scopes = null)
        {
            _scopes = scopes == null || scopes.Count == 0
                ? DEFAULT_SCOPES
                : scopes.ToList();
        }

        public Task<bool> Delete(Scope scope)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            var sc = _scopes.FirstOrDefault(s => s.Name == scope.Name);
            if (sc == null)
            {
                return Task.FromResult(false);
            }

            _scopes.Remove(sc);
            return Task.FromResult(true);
        }

        public Task<ICollection<Scope>> GetAll()
        {
            ICollection<Scope> res = _scopes.ToList();
            return Task.FromResult(res);
        }

        public Task<Scope> Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var scope = _scopes.FirstOrDefault(s => s.Name == name);
            if (scope == null)
            {
                return Task.FromResult((Scope)null);
            }

            return Task.FromResult(scope);
        }

        public Task<bool> Insert(Scope scope)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            scope.CreateDateTime = DateTime.UtcNow;
            _scopes.Add(scope);
            return Task.FromResult(true);
        }

        public Task<SearchScopeResult> Search(SearchScopesParameter parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            IEnumerable<Scope> result = _scopes;
            if (parameter.ScopeNames != null && parameter.ScopeNames.Any())
            {
                result = result.Where(c => parameter.ScopeNames.Any(n => c.Name.Contains(n)));
            }

            if (parameter.Types != null && parameter.Types.Any())
            {
                var scopeTypes = parameter.Types.Select(t => (ScopeType)t);
                result = result.Where(s => scopeTypes.Contains(s.Type));
            }

            var nbResult = result.Count();
            if (parameter.Order != null)
            {
                switch (parameter.Order.Target)
                {
                    case "update_datetime":
                        switch (parameter.Order.Type)
                        {
                            case OrderTypes.Asc:
                                result = result.OrderBy(c => c.UpdateDateTime);
                                break;
                            case OrderTypes.Desc:
                                result = result.OrderByDescending(c => c.UpdateDateTime);
                                break;
                        }
                        break;
                }
            }
            else
            {
                result = result.OrderByDescending(c => c.UpdateDateTime);
            }

            if (parameter.IsPagingEnabled)
            {
                result = result.Skip(parameter.StartIndex).Take(parameter.Count);
            }

            return Task.FromResult(new SearchScopeResult
            {
                Content = result,
                StartIndex = parameter.StartIndex,
                TotalResults = nbResult
            });
        }

        public Task<ICollection<Scope>> SearchByNames(IEnumerable<string> names)
        {
            if (names == null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            ICollection<Scope> result = _scopes.Where(s => names.Contains(s.Name)).ToList();
            return Task.FromResult(result);
        }

        public Task<bool> Update(Scope scope)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            var sc = _scopes.FirstOrDefault(s => s.Name == scope.Name);
            if (sc == null)
            {
                return Task.FromResult(false);
            }

            sc.Claims = scope.Claims;
            sc.Description = scope.Description;
            sc.IsDisplayedInConsent = scope.IsDisplayedInConsent;
            sc.IsExposed = scope.IsExposed;
            sc.IsOpenIdScope = scope.IsOpenIdScope;
            sc.Type = scope.Type;
            sc.UpdateDateTime = DateTime.UtcNow;
            return Task.FromResult(true);
        }
    }
}
