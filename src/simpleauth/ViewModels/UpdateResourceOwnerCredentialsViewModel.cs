﻿namespace SimpleAuth.ViewModels
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Mvc.ModelBinding;

    public class UpdateResourceOwnerCredentialsViewModel
    {
        public string Login { get; set; }
        [Required]
        public string Password { get; set; }      
        [Required]
        public string RepeatPassword { get; set; }

        public void Validate(ModelStateDictionary modelState)
        {
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            if (Password != RepeatPassword)
            {
                modelState.AddModelError("NotSamePassword", "The password must be the same");
            }
        }
    }
}