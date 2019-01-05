﻿namespace SimpleAuth.Tests.Helpers
{
    using Errors;
    using Exceptions;
    using SimpleAuth.Helpers;
    using System.Collections.Generic;
    using Xunit;

    public class AmrHelperFixture
    {
        private IAmrHelper _amrHelper;

        [Fact]
        public void When_No_Amr_Then_Exception_Is_Thrown()
        {
            InitializeFakeObjects();

            var exception = Assert.Throws<SimpleAuthException>(() => _amrHelper.GetAmr(new List<string>(), new[] { "pwd" }));
            Assert.NotNull(exception);
            Assert.Equal(ErrorCodes.InternalError, exception.Code);
            Assert.Equal(ErrorDescriptions.NoActiveAmr, exception.Message);
        }

        [Fact]
        public void When_Amr_Does_Not_Exist_Then_Exception_Is_Thrown()
        {
            InitializeFakeObjects();

            var exception = Assert.Throws<SimpleAuthException>(() => _amrHelper.GetAmr(new List<string> { "invalid" }, new[] { "pwd" }));
            Assert.NotNull(exception);
            Assert.Equal(ErrorCodes.InternalError, exception.Code);
            Assert.Equal(string.Format(ErrorDescriptions.TheAmrDoesntExist, "pwd"), exception.Message);
        }

        [Fact]
        public void When_Amr_Does_Not_Exist_Then_Default_One_Is_Returned()
        {
            InitializeFakeObjects();

            var amr = _amrHelper.GetAmr(new List<string> { "pwd" }, new[] { "invalid" });

            Assert.Equal("pwd", amr);
        }

        [Fact]
        public void When_Amr_Exists_Then_Same_Amr_Is_Returned()
        {
            InitializeFakeObjects();

            var amr = _amrHelper.GetAmr(new List<string> { "amr" }, new[] { "amr" });

            Assert.Equal("amr", amr);
        }

        private void InitializeFakeObjects()
        {
            _amrHelper = new AmrHelper();
        }
    }
}
