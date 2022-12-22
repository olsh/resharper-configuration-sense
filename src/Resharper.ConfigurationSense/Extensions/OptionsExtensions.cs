using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;
using JetBrains.Application.Settings;

using Newtonsoft.Json;

using Resharper.ConfigurationSense.Settings;

namespace Resharper.ConfigurationSense.Extensions
{
    public static class SettingsExtensions
    {
        [NotNull]
        public static IEnumerable<string> GetAdditionalConfigurationFiles(
            this IContextBoundSettingsStore optionsContext,
            string solutionId)
        {
            var customConfigurationFilesJson = optionsContext.GetIndexedValue(
                ConfigurationSenseSettingsAccessor.CustomConfigurationFiles,
                solutionId);

            if (string.IsNullOrEmpty(customConfigurationFilesJson))
            {
                return Enumerable.Empty<string>();
            }

            try
            {
                return JsonConvert.DeserializeObject<HashSet<string>>(customConfigurationFilesJson)
                       ?? Enumerable.Empty<string>();
            }
            // ReSharper disable once CatchAllClause
            catch (Exception)
            {
                optionsContext.SetIndexedValue(
                    ConfigurationSenseSettingsAccessor.CustomConfigurationFiles,
                    solutionId,
                    string.Empty);
                return Enumerable.Empty<string>();
            }
        }

        public static void SaveCustomConfigurationFiles(
            this IContextBoundSettingsStore optionsContext,
            string solutionId,
            IEnumerable<string> customConfigurationFiles)
        {
            optionsContext.SetIndexedValue(
                ConfigurationSenseSettingsAccessor.CustomConfigurationFiles,
                solutionId,
                JsonConvert.SerializeObject(customConfigurationFiles));
        }
    }
}
