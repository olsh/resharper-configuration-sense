using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.Util;

using Resharper.ConfigurationSense.Components;
using Resharper.ConfigurationSense.Constants;
using Resharper.ConfigurationSense.Extensions;

namespace Resharper.ConfigurationSense.SuggestionProviders
{
    [Language(typeof(CSharpLanguage))]
    public class AppSettingsSuggestionProvider : ItemsProviderOfSpecificContext<CSharpCodeCompletionContext>
    {
        private readonly IGenericSettingsProvider _genericSettingsProvider;

        public AppSettingsSuggestionProvider(IGenericSettingsProvider genericSettingsProvider)
        {
            _genericSettingsProvider = genericSettingsProvider;
        }

        protected override bool AddLookupItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
        {
            var lookupItems = _genericSettingsProvider.GetXmlSettingsLookupItems(
                context,
                SettingsConstants.AppSettingsTagName,
                SettingsConstants.AppSettingsKeyAttribute,
                SettingsConstants.AppSettingsValueAttribute);

            if (!lookupItems.Any())
            {
                return false;
            }

            foreach (var lookupItem in lookupItems)
            {
                collector.Add(lookupItem);
            }

            return true;
        }

        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            return context.IsInsideAccessorPath(ClrTypeConstants.AppSettingsPath);
        }
    }
}
