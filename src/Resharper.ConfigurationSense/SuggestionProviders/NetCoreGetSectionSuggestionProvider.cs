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
    public class NetCoreGetSectionSuggestionProvider :
        ItemsProviderOfSpecificContext<CSharpCodeCompletionContext>
    {
        private readonly IGenericSettingsProvider _settingsProvider;

        public NetCoreGetSectionSuggestionProvider(IGenericSettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        protected override bool AddLookupItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
        {
            LogEvent.CreateWithMessage(LoggingLevel.WARN, "Demo", "Shit");

            var lookupItems = _settingsProvider.GetJsonSettingsLookupItems(context, JsonSettingType.Object);

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
            return context.IsInsideMethodPath(ClrTypeConstants.NetCoreGetSection);
        }
    }
}