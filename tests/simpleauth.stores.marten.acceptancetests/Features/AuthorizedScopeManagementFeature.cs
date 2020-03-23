﻿namespace SimpleAuth.Stores.Marten.AcceptanceTests.Features
{
    using SimpleAuth.Shared.Models;
    using Xbehave;
    using Xunit;

    public class AuthorizedScopeManagementFeature : AuthorizedManagementFeatureBase
    {
        [Scenario]
        public void SuccessScopeLoad()
        {
            Scope scope = null;

            "When requesting existing scope".x(
                async () =>
                {
                    var response = await _managerClient.GetScope("test", _grantedToken.AccessToken)
                        .ConfigureAwait(false);

                    Assert.False(response.HasError);

                    scope = response.Content;

                    Assert.NotNull(scope);
                });

            "then scope information is returned".x(() => { Assert.Equal("test", scope.Name); });
        }
    }
}
