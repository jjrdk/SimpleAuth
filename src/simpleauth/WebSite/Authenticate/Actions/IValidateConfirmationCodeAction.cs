﻿namespace SimpleAuth.WebSite.Authenticate.Actions
{
    using System.Threading.Tasks;

    public interface IValidateConfirmationCodeAction
    {
        Task<bool> Execute(string code);
    }
}