﻿using SimpleIdentityServer.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Core.Repositories
{
    using SimpleAuth.Jwt;
    using SimpleAuth.Shared.Models;
    using SimpleAuth.Shared.Parameters;
    using SimpleAuth.Shared.Repositories;
    using SimpleAuth.Shared.Results;

    internal sealed class DefaultClaimRepository : IClaimRepository
    {
        public ICollection<ClaimAggregate> _claims;

        private readonly List<ClaimAggregate> DEFAULT_CLAIMS = new List<ClaimAggregate>
        {
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.Subject, IsIdentifier = true },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.Name },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.FamilyName },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.GivenName },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.MiddleName },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.NickName },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.PreferredUserName },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.Profile },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.Picture },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.WebSite },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.Gender },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.BirthDate },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.ZoneInfo },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.Locale },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.UpdatedAt },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.Email },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.EmailVerified },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.Address },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.PhoneNumber },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.PhoneNumberVerified },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.Role },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.ScimId },
            new ClaimAggregate { Code = JwtConstants.StandardResourceOwnerClaimNames.ScimLocation }
        };

        public DefaultClaimRepository(IReadOnlyCollection<ClaimAggregate> claims = null)
        {
            _claims = (claims == null || claims.Count == 0)
            ? _claims = DEFAULT_CLAIMS
            : _claims = claims.ToList();
        }

        public Task<bool> Delete(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException(nameof(code));
            }

            var claim = _claims.FirstOrDefault(c => c.Code == code);
            if (claim == null)
            {
                return Task.FromResult(false);
            }

            _claims.Remove(claim);
            return Task.FromResult(true);
        }

        public Task<IEnumerable<ClaimAggregate>> GetAllAsync()
        {
            return Task.FromResult(_claims.Select(c => c.Copy()));
        }

        public Task<ClaimAggregate> GetAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var res = _claims.FirstOrDefault(c => c.Code == name);
            if (res == null)
            {
                return Task.FromResult((ClaimAggregate)null);
            }

            return Task.FromResult(res.Copy());
        }

        public Task<bool> InsertAsync(AddClaimParameter claim)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            _claims.Add(new ClaimAggregate
            {
                Code = claim.Code,
                CreateDateTime = DateTime.UtcNow,
                UpdateDateTime = DateTime.UtcNow
            });
            return Task.FromResult(true);
        }

        public Task<SearchClaimsResult> Search(SearchClaimsParameter parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            IEnumerable<ClaimAggregate> result = _claims;
            if (parameter.ClaimKeys != null)
            {
                result = result.Where(c => parameter.ClaimKeys.Any(ck => c.Code.Contains(ck)));
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

            return Task.FromResult(new SearchClaimsResult
            {
                Content = result.Select(c => c.Copy()),
                StartIndex = parameter.StartIndex,
                TotalResults = nbResult
            });
        }

        public Task<bool> Update(ClaimAggregate claim)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            var result = _claims.FirstOrDefault(c => c.Code == claim.Code);
            if (result == null)
            {
                return Task.FromResult(false);
            }

            result.Value = claim.Value;
            result.UpdateDateTime = claim.UpdateDateTime;
            return Task.FromResult(true);
        }
    }
}
