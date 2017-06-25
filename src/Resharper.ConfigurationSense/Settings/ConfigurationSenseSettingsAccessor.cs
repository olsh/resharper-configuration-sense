using System;
using System.Linq.Expressions;

using JetBrains.Annotations;
using JetBrains.Application.Settings.Store;

namespace Resharper.ConfigurationSense.Settings
{
    public class ConfigurationSenseSettingsAccessor
    {
        [NotNull]
        public static readonly Expression<Func<ConfigurationSenseSettings, IIndexedEntry<string, string>>> CustomConfigurationFiles = x => x.CustomConfigurationFiles;
    }
}
