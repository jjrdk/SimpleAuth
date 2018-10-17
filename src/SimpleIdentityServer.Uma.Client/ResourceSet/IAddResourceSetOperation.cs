﻿namespace SimpleIdentityServer.Uma.Client.ResourceSet
{
    using System.Threading.Tasks;
    using Common.DTOs;
    using Results;

    public interface IAddResourceSetOperation
    {
        Task<AddResourceSetResult> ExecuteAsync(PostResourceSet request, string url, string token);
    }
}