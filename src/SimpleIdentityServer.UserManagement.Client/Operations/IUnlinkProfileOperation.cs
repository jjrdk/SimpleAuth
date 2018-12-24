﻿namespace SimpleIdentityServer.UserManagement.Client.Operations
{
    using System.Threading.Tasks;
    using SimpleAuth.Shared;

    public interface IUnlinkProfileOperation
    {
        Task<BaseResponse> Execute(string requestUrl, string externalSubject, string currentSubject, string authorizationHeaderValue = null);
        Task<BaseResponse> Execute(string requestUrl, string externalSubject, string authorizationHeaderValue = null);
    }
}