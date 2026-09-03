using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Controls.FileSystem;
using JetBrains.Application.UI.Icons.CommonThemedIcons;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionPages;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.DataFlow;
using JetBrains.IDE.UI;
using JetBrains.IDE.UI.Extensions;
using JetBrains.IDE.UI.Options;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model.UIAutomation;
using JetBrains.Util;

using Resharper.ConfigurationSense.Extensions;

namespace Resharper.ConfigurationSense.Settings
{
    [OptionsPage(PageId, "Configuration Sense", typeof(BulbThemedIcons.YellowBulbVS), ParentId = EnvironmentPage.Pid)]
    public sealed class ConfigurationSenseOptionsPage : BeSimpleOptionsPage
    {
        private const string PageId = "Configuration Sense";

        [CanBeNull] private readonly ISolution mySolution;

        [CanBeNull] private readonly ListEvents<ConfigurationFileItem> myConfigurationFiles;

        public ConfigurationSenseOptionsPage(
            Lifetime lifetime,
            OptionsPageContext optionsPageContext,
            [NotNull] OptionsSettingsSmartContext optionsSettingsSmartContext,
            IIconHost iconHost,
            IShellLocks shellLocks,
            ICommonFileDialogs commonFileDialogs,
            [CanBeNull] ISolution solution = null)
            : base(lifetime, optionsPageContext, optionsSettingsSmartContext)
        {
            if (solution == null)
            {
                AddHeader("To edit the list of additional configuration files you should open a solution.");
                return;
            }

            mySolution = solution;
            myConfigurationFiles = ListEvents<ConfigurationFileItem>.Create("ConfigurationSense::ConfigurationFiles");
            var selectedFile = new Property<ConfigurationFileItem>("ConfigurationSense::SelectedConfigurationFile");

            using (ReadLockCookie.Create())
            {
                foreach (var persistentId in optionsSettingsSmartContext.GetAdditionalConfigurationFiles(
                             solution.GetId()))
                {
                    var element = solution.FindElementByPersistentID(persistentId);
                    if (element == null)
                    {
                        continue;
                    }

                    myConfigurationFiles.Add(new ConfigurationFileItem(persistentId, element.Name));
                }
            }

            var toolbar = selectedFile.GetBeSingleSelectionListWithToolbar(
                myConfigurationFiles,
                lifetime,
                (_, item, _) => new List<BeControl> { item.Presentation.GetBeLabel() },
                iconHost,
                new[] { "File,*" },
                hasHeader: false);

            var solutionPath = solution.SolutionFilePath.IsEmpty
                ? solution.SolutionDirectory
                : solution.SolutionFilePath;
            var addButton = BeControls.GetPathSelectionButton(
                string.Empty.GetBeLabel(CommonThemedIcons.Create.Id.GetIcon(iconHost)),
                BrowsePathOptions.OpenFile,
                lifetime,
                commonFileDialogs,
                solutionPath.ToNativeFileSystemPath(),
                new[] { new ChooseFileType(new[] { "json", "xml" }, "Configuration files") },
                BeButtonStyle.ICON,
                path => shellLocks.ExecuteOrQueueReadLockEx(
                    lifetime,
                    "ConfigurationSense::AddConfigurationFile",
                    () => AddConfigurationFile(path)));

            toolbar
                .AddItem(addButton)
                .AddButtonWithListAction<ConfigurationFileItem>(
                    BeListAction.REMOVE,
                    index => myConfigurationFiles.RemoveAt(index));

            AddHeader("Configuration files");
            AddCommentText(
                "You don't need to add appsettings.json, web.config and app.config (and transformation files) to the list, they're scanned by default.");
            AddCommentText("Only *.json and *.xml files are supported.");
            AddControl(toolbar, isStar: true);
        }

        public override bool OnOk()
        {
            if (mySolution != null && myConfigurationFiles != null)
            {
                var persistentIds = new HashSet<string>(myConfigurationFiles.Select(file => file.PersistentId));
                OptionsSettingsSmartContext.SaveCustomConfigurationFiles(mySolution.GetId(), persistentIds);
            }

            return base.OnOk();
        }

        private void AddConfigurationFile(FileSystemPath path)
        {
            var virtualPath = path.FullPath.ParseVirtualPathSafe(InteractionContext.SolutionContext);
            if (virtualPath.IsEmpty)
            {
                return;
            }

            var projectFile = mySolution.FindProjectItemsByLocation(virtualPath)
                .OfType<IProjectFile>()
                .FirstOrDefault();
            if (projectFile == null
                || (!projectFile.LanguageType.Is<JsonProjectFileType>()
                    && !projectFile.LanguageType.Is<XmlProjectFileType>()))
            {
                return;
            }

            var persistentId = projectFile.GetPersistentID();
            if (myConfigurationFiles.Any(file => file.PersistentId == persistentId))
            {
                return;
            }

            myConfigurationFiles.Add(new ConfigurationFileItem(persistentId, projectFile.Name));
        }

        private sealed class ConfigurationFileItem
        {
            public ConfigurationFileItem([NotNull] string persistentId, [NotNull] string presentation)
            {
                PersistentId = persistentId;
                Presentation = presentation;
            }

            [NotNull]
            public string PersistentId { get; }

            [NotNull]
            public string Presentation { get; }
        }
    }
}
