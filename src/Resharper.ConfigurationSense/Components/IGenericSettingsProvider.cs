using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using Resharper.ConfigurationSense.Constants;
using Resharper.ConfigurationSense.Models;

namespace Resharper.ConfigurationSense.Components
{
    public interface IGenericSettingsProvider
    {
        LinkedList<KeyValueSettingLookupItem> GetJsonSettingsLookupItems(
            ISpecificCodeCompletionContext context,
            JsonSettingType settingType,
            string jsonPath = null);

        LinkedList<KeyValueSettingLookupItem> GetXmlSettingsLookupItems(
            ISpecificCodeCompletionContext context,
            string settingsTagName,
            string settingsKeyAttribute,
            string settingsValueAttributes);
    }
}
