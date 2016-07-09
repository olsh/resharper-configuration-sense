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
    public class ConnectionStringsSuggestionProvider : ItemsProviderOfSpecificContext<CSharpCodeCompletionContext>
    {
        #region Fields

        private readonly IClrTypeName _connectionStringsClrName =
            new ClrTypeName(ClrTypeConstants.ConnectionStringsClrType);

        private readonly IGenericKeyValueSettingsProvider _keyValueSettingsProvider;

        #endregion

        #region Constructors

        public ConnectionStringsSuggestionProvider(IGenericKeyValueSettingsProvider keyValueSettingsProvider)
        {
            _keyValueSettingsProvider = keyValueSettingsProvider;
        }

        #endregion

        #region Methods

        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            return context.IsInsideElement(_connectionStringsClrName);
        }

        protected override bool AddLookupItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
        {
            var lookupItems = _keyValueSettingsProvider.GetLookupItems(
                context, 
                SettingsConstants.ConnectionStringTagName, 
                SettingsConstants.ConnectionStringsKeyAttribute, 
                SettingsConstants.ConnectionStringsValueAttribute);

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
