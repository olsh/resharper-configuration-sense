using System.Collections.Generic;

using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.Application.UI.Components;
using JetBrains.Application.UI.Controls.StringCollectionEdit.Impl;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionPages;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Feature.Services.Util.FilesAndDirs;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.UnitTestFramework.UI.Options;

using Resharper.ConfigurationSense.Extensions;

namespace Resharper.ConfigurationSense.Settings
{
    [OptionsPage(PageId, "Configuration Sense", typeof(BulbThemedIcons.YellowBulbVS), ParentId = EnvironmentPage.Pid)]
    public sealed class ConfigurationSenseOptionsPage : BetterCustomOptionsPage
    {
        private const string PageId = "Configuration Sense";

        public ConfigurationSenseOptionsPage(
            Lifetime lifetime,
            [NotNull] OptionsSettingsSmartContext optionsSettingsSmartContext,
            IComponentContainer componentContainer,
            ProjectModelElementPresentationService presentationService,
            IUIApplication iuiApplication)
            : base(lifetime, optionsSettingsSmartContext)
        {
            var solution = componentContainer.TryGetComponent<ISolution>();
            if (solution == null)
            {
                AddHeader("To edit the list of additional configuration files you should open a solution.");
                return;
            }

            var editItemViewModelFactory = new FilesAndDirsCollectionEditItemViewModelFactory(iuiApplication.ShellLocks, presentationService);
            var buttonProviderFactory = new FilesAndDirsButtonProviderFactory(
                lifetime,
                iuiApplication.ShellLocks,
                solution,
                editItemViewModelFactory,
                false);

            var customConfigurationFiles = new StringCollectionEditViewModel(
                lifetime,
                @"Additional configuration files.
You don't need to add appsettings.json, web.config and app.config (and transformation files) to the list, they're scanned by default.
Only *.json and *.xml files are supported.",
                buttonProviderFactory,
                editItemViewModelFactory);

            using (ReadLockCookie.Create())
            {
                foreach (var x in optionsSettingsSmartContext.GetAdditionalConfigurationFiles(solution.GetId()))
                {
                    var element = solution.FindElementByPersistentID(x);
                    if (element == null)
                    {
                        continue;
                    }

                    customConfigurationFiles.AddItem(element.Name, behindValue: element);
                }
            }

            customConfigurationFiles.Items.CollectionChanged += (_, _) =>
                {
                    var hashSet = new HashSet<string>();
                    foreach (var item in customConfigurationFiles.Items)
                    {
                        var filesAndDirsItem = (FilesAndDirsItemViewModel)item;
                        if (filesAndDirsItem.ProjectElement is IProjectFile projectFile
                            && (projectFile.LanguageType.Is<JsonProjectFileType>()
                                || projectFile.LanguageType.Is<XmlProjectFileType>()))
                        {
                            hashSet.Add(projectFile.GetPersistentID());
                        }
                    }

                    OptionsSettingsSmartContext.SaveCustomConfigurationFiles(solution.GetId(), hashSet);
                };

            AddHeader("Configuration files");
            AddCustomOption(customConfigurationFiles);
        }
    }
}
