﻿namespace SimpleAuth.Uma.Tests.Services
{
    using SimpleAuth.Shared;

    internal sealed class DefaultEventPublisher : IEventPublisher
    {
        public void Publish<T>(T evt) where T : Event
        {
        }
    }
}
