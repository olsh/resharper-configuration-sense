using System.Collections.Generic;

using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;

using Resharper.ConfigSense.Models;

namespace Resharper.ConfigSense.Components
{
    public interface IGenericKeyValueSettingsProvider
    {
        LinkedList<KeyValueSettingLookupItem> GetLookupItems(
            CSharpCodeCompletionContext context, 
            string settingsTagName, 
            string settingsKeyAttribute, 
            string settingsValueAttributes);
    }
}