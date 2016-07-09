using System;
using System.Collections.Generic;

using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;

using NuGet;

using Resharper.ConfigurationSense.Constants;
using Resharper.ConfigurationSense.Models;

namespace Resharper.ConfigurationSense.Extensions
{
    public static class ProjectExtensions
    {
        #region Methods

        public static IEnumerable<KeyValueSetting> GetProjectSettings(
            this IProject project, 
            string settingsTagName, 
            string settingsKeyAttribute, 
            string settingsValueAttributes)
        {
            var result = new LinkedList<KeyValueSetting>();

            var configFiles = GetConfigFiles(project);

            foreach (var configFile in configFiles)
            {
                var settingsTags = new Queue<IXmlTag>(GetSettingTags(settingsTagName, configFile));
                while (settingsTags.Count > 0)
                {
                    var settingTag = settingsTags.Dequeue();

                    var configName = settingTag.GetExternalConfigName();
                    if (!string.IsNullOrEmpty(configName))
                    {
                        var projectItems =
                            project.FindProjectItemsByLocation(configFile.Location.Directory.Combine(configName));
                        foreach (var projectItem in projectItems)
                        {
                            var projectFile = projectItem as IProjectFile;
                            var externalSettingsTags = GetSettingTags(settingsTagName, projectFile);
                            foreach (var settingsTag in externalSettingsTags)
                            {
                                settingsTags.Enqueue(settingsTag);
                            }
                        }

                        continue;
                    }

                    var settings = settingTag.GetChildSettings(settingsKeyAttribute, settingsValueAttributes);
                    result.AddRange(settings);
                }
            }

            return result;
        }

        private static IEnumerable<IXmlTag> GetSettingTags(string settingsTagName, IProjectFile configFile)
        {
            var xmlFile = configFile.GetPrimaryPsiFile() as XmlFile;
            if (xmlFile == null)
            {
                return new List<IXmlTag>();
            }

            var collection = xmlFile.FindAllByTagName(settingsTagName);
            return collection;
        }

        private static IEnumerable<IProjectFile> GetConfigFiles(IProject project)
        {
            return
                project.GetAllProjectFiles(
                    file =>
                    file.Name.Equals(FileConstants.WebConfigFileName, StringComparison.OrdinalIgnoreCase)
                    || file.Name.Equals(FileConstants.AppConfigFileName, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}
