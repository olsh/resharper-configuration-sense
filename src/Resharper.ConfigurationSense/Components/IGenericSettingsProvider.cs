using System.Collections.Generic;

using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;

using Resharper.ConfigurationSense.Models;

namespace Resharper.ConfigurationSense.Components
{
    public interface IGenericSettingsProvider
    {
        LinkedList<KeyValueSettingLookupItem> GetJsonSettingsLookupItems(
            CSharpCodeCompletionContext context,
            string fileName,
            string jsonPath = null);

        LinkedList<KeyValueSettingLookupItem> GetXmlSettingsLookupItems(
            CSharpCodeCompletionContext context,
            string settingsTagName,
            string settingsKeyAttribute,
            string settingsValueAttributes);
    }
}
