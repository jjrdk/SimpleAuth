﻿// Copyright © 2015 Habart Thierry, © 2018 Jacob Reimers
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Core.Translation
{
    using Shared.Repositories;

    public class TranslationManager : ITranslationManager
    {
        private readonly OAuthConfigurationOptions _configurationService;
        private readonly ITranslationRepository _translationRepository;

        public TranslationManager(
            OAuthConfigurationOptions configurationService,
            ITranslationRepository translationRepository)
        {
            _configurationService = configurationService;
            _translationRepository = translationRepository;
        }

        /// <summary>
        /// Get the translation by order of preferrence.
        /// </summary>
        /// <param name="concatenateListOfCodeLanguages"></param>
        /// <param name="translationCodes"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, string>> GetTranslationsAsync(string concatenateListOfCodeLanguages, List<string> translationCodes)
        {
            if (translationCodes == null)
            {
                throw new ArgumentNullException(nameof(translationCodes));
            }

            var preferredLanguage = await GetPreferredLanguage(concatenateListOfCodeLanguages).ConfigureAwait(false);
            var result = new Dictionary<string, string>();
            foreach (var translationCode in translationCodes)
            {
                var record = await _translationRepository.GetAsync(preferredLanguage, translationCode).ConfigureAwait(false);
                if (record != null)
                {
                    result.Add(record.Code, record.Value);
                }
                else
                {
                    result.Add(translationCode, string.Format("[{0}]", translationCode));
                }
            }

            return result;
        }

        private async Task<string> GetPreferredLanguage(string concatenateListOfCodeLanguages)
        {
            if (string.IsNullOrWhiteSpace(concatenateListOfCodeLanguages))
            {
                return _configurationService.DefaultLanguage.TwoLetterISOLanguageName;
            }

            var listOfCodeLanguages = concatenateListOfCodeLanguages.Split(' ');
            var supportedCodeLanguages = await _translationRepository.GetLanguageTagsAsync().ConfigureAwait(false);
            if (!listOfCodeLanguages.Any()
                || supportedCodeLanguages == null
                || !supportedCodeLanguages.Any())
            {
                return _configurationService.DefaultLanguage.TwoLetterISOLanguageName;
            }

            foreach (var codeLanguage in listOfCodeLanguages)
            {
                if (supportedCodeLanguages.Contains(codeLanguage))
                {
                    return codeLanguage;
                }
            }

            return _configurationService.DefaultLanguage.TwoLetterISOLanguageName;
        }
    }
}
