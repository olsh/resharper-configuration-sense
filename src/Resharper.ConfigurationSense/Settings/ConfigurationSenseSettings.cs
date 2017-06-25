using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Store;

namespace Resharper.ConfigurationSense.Settings
{
    [SettingsKey(typeof(EnvironmentSettings), "Configuration Sense plugin settings")]
    public sealed class ConfigurationSenseSettings
    {
        [SettingsIndexedEntry("Custom configuration files")]
        public IIndexedEntry<string, string> CustomConfigurationFiles { get; set; }
    }
}
