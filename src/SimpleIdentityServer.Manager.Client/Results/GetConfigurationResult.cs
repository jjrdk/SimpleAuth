﻿namespace SimpleIdentityServer.Manager.Client.Results
{
    using Shared;
    using Shared.Responses;

    public class GetConfigurationResult : BaseResponse
    {
	    public ConfigurationResponse Content { get; set; }
    }
}
