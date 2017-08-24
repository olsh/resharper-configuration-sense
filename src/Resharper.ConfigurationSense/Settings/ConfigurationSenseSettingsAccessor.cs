using System;
using System.Linq.Expressions;

using JetBrains.Annotations;
using JetBrains.Application.Settings;

namespace Resharper.ConfigurationSense.Settings
{
    public static class ConfigurationSenseSettingsAccessor
    {
        [NotNull]
        public static readonly Expression<Func<ConfigurationSenseSettings, IIndexedEntry<string, string>>> CustomConfigurationFiles = x => x.CustomConfigurationFiles;
    }
}
