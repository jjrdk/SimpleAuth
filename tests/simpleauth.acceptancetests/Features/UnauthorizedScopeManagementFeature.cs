﻿namespace SimpleAuth.AcceptanceTests.Features
{
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Models;
    using System.Net;
    using Xbehave;
    using Xunit;
    using Xunit.Abstractions;

    public class UnauthorizedScopeManagementFeature : UnauthorizedManagementFeatureBase
    {
        /// <inheritdoc />
        public UnauthorizedScopeManagementFeature(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Scenario]
        public void RejectedScopeLoad()
        {
            Option<Scope>.Error scope = null;

            "When requesting existing scope".x(
                async () =>
                {
                    scope = await _managerClient.GetScope("test", _grantedToken.AccessToken)
                        .ConfigureAwait(false) as Option<Scope>.Error;
                });

            "then error is returned".x(() => { Assert.Equal(HttpStatusCode.Forbidden, scope.Details.Status); });
        }

        [Scenario]
        public void RejectedAddScope()
        {
            Option<Scope>.Error scope = null;

            "When adding new scope".x(
                async () =>
                {
                    scope = await _managerClient.AddScope(
                            new Scope {Name = "test", Claims = new[] {"openid"}},
                            _grantedToken.AccessToken)
                        .ConfigureAwait(false) as Option<Scope>.Error;
                });

            "then error is returned".x(() => { Assert.Equal(HttpStatusCode.Forbidden, scope.Details.Status); });
        }
    }
}
