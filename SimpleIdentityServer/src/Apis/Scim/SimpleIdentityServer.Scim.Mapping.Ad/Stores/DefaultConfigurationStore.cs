﻿using Newtonsoft.Json;
using SimpleIdentityServer.Scim.Mapping.Ad.Models;
using System;
using System.IO;

namespace SimpleIdentityServer.Scim.Mapping.Ad.Stores
{
    internal sealed class DefaultConfigurationStore : IConfigurationStore
    {
        private static string _fileName = "AdConfiguration.json";

        public DefaultConfigurationStore(AdConfiguration adConfiguration)
        {
            if (adConfiguration != null)
            {
                UpdateConfiguration(adConfiguration);
            }
        }

        public bool UpdateConfiguration(AdConfiguration adConfiguration)
        {
            if(adConfiguration == null)
            {
                throw new ArgumentNullException(nameof(adConfiguration));
            }

            var fullPath = GetFullPath();
            if(File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            File.WriteAllText(fullPath, JsonConvert.SerializeObject(adConfiguration));
            return true;
        }

        public AdConfiguration GetConfiguration()
        {
            var fullPath = GetFullPath();
            if(!File.Exists(fullPath))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<AdConfiguration>(File.ReadAllText(fullPath));
        }

        private static string GetFullPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), _fileName);
        }
    }
}