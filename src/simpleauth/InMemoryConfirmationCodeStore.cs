﻿namespace SimpleAuth
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal sealed class InMemoryConfirmationCodeStore : IConfirmationCodeStore
    {
        private readonly ICollection<ConfirmationCode> _confirmationCodes;

        public InMemoryConfirmationCodeStore()
        {
            _confirmationCodes = new List<ConfirmationCode>();
        }

        public Task<bool> Add(ConfirmationCode confirmationCode)
        {
            if (confirmationCode == null)
            {
                throw new ArgumentNullException(nameof(confirmationCode));
            }

            _confirmationCodes.Add(confirmationCode);
            return Task.FromResult(true);
        }

        public Task<ConfirmationCode> Get(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException(nameof(code));
            }

            return Task.FromResult(_confirmationCodes.FirstOrDefault(c => c.Value == code));
        }

        public Task<bool> Remove(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException(nameof(code));
            }

            var confirmationCode = _confirmationCodes.FirstOrDefault(c => c.Value == code);
            if (confirmationCode == null)
            {
                return Task.FromResult(false);
            }

            _confirmationCodes.Remove(confirmationCode);
            return Task.FromResult(true);
        }
    }
}
