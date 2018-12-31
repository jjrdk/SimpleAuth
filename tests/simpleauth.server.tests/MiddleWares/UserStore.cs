﻿namespace SimpleAuth.Server.Tests.MiddleWares
{
    using System;

    public class UserStore
    {
        private static UserStore _instance;
        private static readonly string DefaultSubject = "administrator";

        private UserStore()
        {
            Subject = DefaultSubject;
        }

        public static UserStore Instance()
        {
            if (_instance == null)
            {
                _instance = new UserStore();
            }

            return _instance;
        }

        public bool IsInactive { get; set; }
        public string Subject { get; set; }
        public DateTimeOffset? AuthenticationOffset { get; set; }
    }
}