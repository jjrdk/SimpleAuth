﻿namespace SimpleAuth.Parameters
{
    using System.Collections.Generic;
    using System.Linq;

    internal record ClaimParameter
    {
        public string Name { get; init; } = null!;

        public Dictionary<string, object> Parameters { get; init; } = new();

        public bool Essential => GetBoolean(CoreConstants.StandardClaimParameterValueNames.EssentialName);

        public string Value => GetString(CoreConstants.StandardClaimParameterValueNames.ValueName);

        public string[]? Values => GetArray(CoreConstants.StandardClaimParameterValueNames.ValuesName);

        public bool EssentialParameterExist => Parameters.Any(p => p.Key == CoreConstants.StandardClaimParameterValueNames.EssentialName);

        public bool ValueParameterExist => Parameters.Any(p => p.Key == CoreConstants.StandardClaimParameterValueNames.ValueName);

        public bool ValuesParameterExist => Parameters.Any(p => p.Key == CoreConstants.StandardClaimParameterValueNames.ValuesName);

        private bool GetBoolean(string name)
        {
            var value = GetString(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return bool.TryParse(value, out var result) && result;
        }

        private string GetString(string name)
        {
            var keyPair = Parameters.FirstOrDefault(p => p.Key == name);
            if (keyPair.Equals(default(KeyValuePair<string, object>))
                || string.IsNullOrWhiteSpace(keyPair.ToString()))
            {
                return string.Empty;
            }

            return keyPair.Value.ToString()!;
        }

        private string[]? GetArray(string name)
        {
            var keyPair = Parameters.FirstOrDefault(p => p.Key == name);
            if (keyPair.Equals(default(KeyValuePair<string, object>))
                || string.IsNullOrWhiteSpace(keyPair.ToString()))
            {
                return null;
            }

            var value = keyPair.Value;
            if (!value.GetType().IsArray)
            {
                return null;
            }

            var result = (object[])value;
            return result.Select(r => r.ToString()!).ToArray();
        }
    }
}