﻿namespace SimpleAuth.Stores.Marten.AcceptanceTests.Features
{
    using SimpleAuth.Shared.Requests;
    using Xbehave;
    using Xunit;

    public class ResourceOwnerManagementFeature : AuthorizedManagementFeatureBase
    {
        [Scenario]
        public void SuccessAddResourceOwner()
        {
            string subject = null;

            "When adding resource owner".x(
                async () =>
                {
                    var response = await _managerClient.AddResourceOwner(
                            new AddResourceOwnerRequest { Password = "test", Subject = "test" },
                            _grantedToken.AccessToken)
                        .ConfigureAwait(false);

                    Assert.False(response.ContainsError);

                    subject = response.Content;
                });

            "Then resource owner is local account".x(
                async () =>
                {
                    var response = await _managerClient.GetResourceOwner(subject, _grantedToken.AccessToken)
                        .ConfigureAwait(false);

                    Assert.True(response.Content.IsLocalAccount);
                });
        }

        [Scenario]
        public void SuccessUpdateResourceOwnerPassword()
        {
            "When adding resource owner".x(
                async () =>
                {
                    var response = await _managerClient.AddResourceOwner(
                            new AddResourceOwnerRequest { Password = "test", Subject = "test" },
                            _grantedToken.AccessToken)
                        .ConfigureAwait(false);

                    Assert.False(response.ContainsError);
                });

            "Then can update resource owner password".x(
                async () =>
                {
                    var response = await _managerClient.UpdateResourceOwnerPassword(
                            new UpdateResourceOwnerPasswordRequest { Subject = "test", Password = "test2" },
                            _grantedToken.AccessToken)
                        .ConfigureAwait(false);

                    Assert.False(response.ContainsError);
                });
        }
    }
}
