using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
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
        #region Fields

        private readonly IClrTypeName _appSettingsClrName = new ClrTypeName(ClrTypeConstants.AppSettingsClrType);

        private readonly IGenericKeyValueSettingsProvider _genericKeyValueSettingsProvider;

        #endregion

        #region Constructors

        public AppSettingsSuggestionProvider(IGenericKeyValueSettingsProvider genericKeyValueSettingsProvider)
        {
            _genericKeyValueSettingsProvider = genericKeyValueSettingsProvider;
        }

        #endregion

        #region Methods

        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            return context.IsInsideElement(_appSettingsClrName);
        }

        protected override bool AddLookupItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
        {
            var lookupItems = _genericKeyValueSettingsProvider.GetLookupItems(
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

        #endregion
    }
}
