using System.Collections.Generic;

using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

using Resharper.ConfigurationSense.Extensions;
using Resharper.ConfigurationSense.Models;

namespace Resharper.ConfigurationSense.Components
{
    [ShellComponent]
    public class GenericSuggestionSettingsProvider : IGenericKeyValueSettingsProvider
    {
        #region Fields

        private readonly ProjectModelElementPresentationService _presentationService;

        #endregion

        #region Constructors

        public GenericSuggestionSettingsProvider(ProjectModelElementPresentationService presentationService)
        {
            _presentationService = presentationService;
        }

        #endregion

        #region Methods

        public LinkedList<KeyValueSettingLookupItem> GetLookupItems(
            CSharpCodeCompletionContext context, 
            string settingsTagName, 
            string settingsKeyAttribute, 
            string settingsValueAttributes)
        {
            var lookupItems = new LinkedList<KeyValueSettingLookupItem>();

            var project = context.BasicContext.File.GetProject();
            if (project == null)
            {
                return lookupItems;
            }

            var rangeMarker =
                new TextRange(context.BasicContext.CaretDocumentRange.TextRange.StartOffset).CreateRangeMarker(
                    context.BasicContext.Document);

            var settings = project.GetProjectSettings(settingsTagName, settingsKeyAttribute, settingsValueAttributes);

            foreach (var setting in settings)
            {
                var iconId = _presentationService.GetIcon(project);
                var lookupItem = new KeyValueSettingLookupItem(setting, iconId, rangeMarker);
                lookupItem.InitializeRanges(context.EvaluateRanges(), context.BasicContext);

                lookupItems.AddLast(lookupItem);
            }

            return lookupItems;
        }

        #endregion
    }
}
