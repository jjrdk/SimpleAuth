﻿// Copyright © 2016 Habart Thierry, © 2018 Jacob Reimers
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace SimpleAuth.Client
{
    using System.Security.Cryptography;
    using System.Text;
    using SimpleAuth.Shared;

    internal static class PasswordExtensions
    {
        public static string ToSha256SimplifiedBase64(this string entry, Encoding? encoding = null)
        {
            var enc = encoding ?? Encoding.UTF8;
            using var sha256 = SHA256.Create();
            var entryBytes = enc.GetBytes(entry);
            var hash = sha256.ComputeHash(entryBytes);
            return hash.ToBase64Simplified();
        }
    }
}
