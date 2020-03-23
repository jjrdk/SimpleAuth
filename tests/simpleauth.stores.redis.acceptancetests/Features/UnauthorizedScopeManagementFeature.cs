﻿namespace SimpleAuth.Stores.Redis.AcceptanceTests.Features
{
    using System.Net;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Models;
    using Xbehave;
    using Xunit;

    public class UnauthorizedScopeManagementFeature : UnauthorizedManagementFeatureBase
    {
        [Scenario]
        public void RejectedScopeLoad()
        {
            GenericResponse<Scope> scope = null;

            "When requesting existing scope".x(
                async () =>
                {
                    scope = await _managerClient.GetScope("test", _grantedToken.AccessToken)
                        .ConfigureAwait(false);
                });

            "then error is returned".x(() => { Assert.Equal(HttpStatusCode.Forbidden, scope.StatusCode); });
        }

        [Scenario]
        public void RejectedAddScope()
        {
            GenericResponse<Scope> scope = null;

            "When adding new scope".x(
                async () =>
                {
                    scope = await _managerClient.AddScope(
                        new Scope { Name = "test", Claims = new[] { "openid" } },
                        _grantedToken.AccessToken)
                    .ConfigureAwait(false);
                });

            "then error is returned".x(() => { Assert.Equal(HttpStatusCode.Forbidden, scope.StatusCode); });
        }
    }
}