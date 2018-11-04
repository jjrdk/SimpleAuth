﻿namespace SimpleIdentityServer.Core.Authenticate
{
    using Shared.Models;

    public interface IClientTlsAuthentication
    {
        Client AuthenticateClient(AuthenticateInstruction instruction, Client client);
    }
}