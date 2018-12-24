﻿namespace SimpleIdentityServer.Host.UserInfo.Actions
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;

    public interface IGetJwsPayload
    {
        Task<IActionResult> Execute(string accessToken);
    }
}