﻿namespace SimpleIdentityServer.Scim.Events
{
    using Common.Dtos;

    public class PatchUserReceived : Event
    {
        public PatchUserReceived(string id, string processId, string payload, int order)
        {
            Id = id;
            ProcessId = processId;
            Payload = payload;
            Order = order;
        }

        public string Id { get; private set; }
        public string ProcessId { get; private set; }
        public string Payload { get; private set; }
        public int Order { get; private set; }
    }
}
