﻿using SimpleBus.Core;
using SimpleIdentityServer.OAuth.Events;
using System;

namespace SimpleBus.RabbitMq.Tests
{
    internal sealed class TokenGrantedHandler : IEventHandler<TokenGranted>
    {
        public void Handle(TokenGranted evt)
        {
            Console.WriteLine("token granted");
        }
    }
}