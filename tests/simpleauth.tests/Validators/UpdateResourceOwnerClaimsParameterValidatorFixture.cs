﻿namespace SimpleAuth.Tests.Validators
{
    using System;
    using Errors;
    using Exceptions;
    using Parameters;
    using SimpleAuth.Validators;
    using Xunit;

    public class UpdateResourceOwnerClaimsParameterValidatorFixture
    {
        private IUpdateResourceOwnerClaimsParameterValidator _updateResourceOwnerClaimsParameterValidator;

        [Fact]
        public void When_Pass_Null_Parameter_Then_Exception_Is_Thrown()
        {            InitializeFakeObjects();

                        Assert.Throws<ArgumentNullException>(() => _updateResourceOwnerClaimsParameterValidator.Validate(null));
        }

        [Fact]
        public void When_Login_Is_Null_Then_Exception_Is_Thrown()
        {            InitializeFakeObjects();

                        var ex = Assert.Throws<SimpleAuthException>(() => _updateResourceOwnerClaimsParameterValidator.Validate(new UpdateResourceOwnerClaimsParameter()));

                        Assert.NotNull(ex);
            Assert.Equal(ErrorCodes.InvalidRequestCode, ex.Code);
            Assert.Equal("the parameter login is missing", ex.Message);
        }

        private void InitializeFakeObjects()
        {
            _updateResourceOwnerClaimsParameterValidator = new UpdateResourceOwnerClaimsParameterValidator();
        }
    }
}
