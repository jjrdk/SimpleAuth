﻿namespace SimpleAuth.Tests.Api.Sms.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using SimpleAuth.Shared;
    using SimpleAuth.Shared.Models;
    using SimpleAuth.Shared.Repositories;
    using SimpleAuth.Sms.Actions;
    using Xunit;

    public class SmsAuthenticationOperationFixture
    {
        private readonly SmsAuthenticationOperation _smsAuthenticationOperation;

        public SmsAuthenticationOperationFixture()
        {
            var generateAndSendSmsCodeOperationStub = new Mock<IConfirmationCodeStore>();
            var resourceOwnerRepositoryStub = new Mock<IResourceOwnerRepository>();
            var subjectBuilderStub = new Mock<ISubjectBuilder>();
            subjectBuilderStub.Setup(x => x.BuildSubject(It.IsAny<IEnumerable<Claim>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(DateTime.UtcNow.Ticks.ToString);
            _smsAuthenticationOperation = new SmsAuthenticationOperation(
                new RuntimeSettings(),
                null,
                generateAndSendSmsCodeOperationStub.Object,
                resourceOwnerRepositoryStub.Object,
                subjectBuilderStub.Object,
                new IAccountFilter[0],
                new Mock<IEventPublisher>().Object);
        }

        [Fact]
        public async Task When_Null_Parameter_Is_Passed_Then_Exception_Is_Thrown()
        {
            await Assert
                .ThrowsAsync<ArgumentNullException>(
                    () => _smsAuthenticationOperation.Execute(null, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task When_Empty_Parameter_Is_Passed_Then_Exception_Is_Thrown()
        {
            await Assert
                .ThrowsAsync<ArgumentNullException>(
                    () => _smsAuthenticationOperation.Execute(string.Empty, CancellationToken.None))
                .ConfigureAwait(false);
        }
    }
}
