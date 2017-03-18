using System;
using System.Collections.Generic;
using System.IO;

using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;

using Newtonsoft.Json.Linq;

using NuGet;

using Resharper.ConfigurationSense.Constants;
using Resharper.ConfigurationSense.Models;

namespace Resharper.ConfigurationSense.Extensions
{
    public static class ProjectExtensions
    {
        public static IEnumerable<KeyValueSetting> GetJsonProjectSettings(
            this IProject project,
            JsonSettingType settingType,
            string searchPath = null)
        {
            var configFiles = GetNetCoreJsonConfigFiles(project);

            var settings = new HashSet<KeyValueSetting>(KeyValueSetting.KeyComparer);
            foreach (var projectFile in configFiles)
            {
                var json = ParseJsonProjectFile(projectFile);

                var properties = new Queue<JProperty>();
                properties.EnqueueRange(json.Properties());

                var secretsJson = ReadSecretsJsonSafe(project);
                if (secretsJson != null)
                {
                    properties.EnqueueRange(secretsJson.Properties());
                }

                while (properties.Count > 0)
                {
                    var property = properties.Dequeue();
                    if (!property.HasValues || string.IsNullOrEmpty(property.Path))
                    {
                        continue;
                    }

                    string formattedPath = null;

                    if (property.Value.Type == JTokenType.Object)
                    {
                        foreach (var nestedProperty in ((JObject)property.Value).Properties())
                        {
                            properties.Enqueue(nestedProperty);
                        }
                    }
                    
                    if ((property.Value.Type == JTokenType.Object && settingType == JsonSettingType.Object)
                        || (property.Value.Type != JTokenType.Object && property.Value.Type != JTokenType.Array
                             && settingType == JsonSettingType.Value))
                    {
                        formattedPath = FormatJsonPath(property);
                    }

                    if (string.IsNullOrEmpty(formattedPath))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(searchPath))
                    {
                        if (formattedPath.StartsWith(searchPath, StringComparison.OrdinalIgnoreCase))
                        {
                            formattedPath = formattedPath.Replace(searchPath, string.Empty);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    settings.Add(new KeyValueSetting { Key = formattedPath, Value = property.Value.ToString() });
                }
            }

            return settings;
        }

        public static IEnumerable<KeyValueSetting> GetXmlProjectSettings(
            this IProject project,
            string settingsTagName,
            string settingsKeyAttribute,
            string settingsValueAttributes)
        {
            var result = new HashSet<KeyValueSetting>(KeyValueSetting.KeyComparer);

            var configFiles = GetXmlConfigFiles(project);

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

        private static string FormatJsonPath(JProperty property)
        {
            var formattedPath = property.Path.Replace(".", ":");
            return formattedPath;
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

        private static IEnumerable<IProjectFile> GetXmlConfigFiles(IProject project)
        {
            return
                project.GetAllProjectFiles(
                    file =>
                        file.Name.Equals(FileNames.WebConfig, StringComparison.OrdinalIgnoreCase)
                        || file.Name.Equals(FileNames.AppConfig, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<IProjectFile> GetNetCoreJsonConfigFiles(IProject project)
        {
            return
                project.GetAllProjectFiles(
                    file =>
                        file.Name.Equals(FileNames.NetCoreAppSettingsJson, StringComparison.OrdinalIgnoreCase));
        }

        private static JObject ParseJsonProjectFile(IProjectFile projectFile)
        {
            var primaryPsiFile = projectFile.GetPrimaryPsiFile();
            var text = primaryPsiFile.GetUnquotedText();
            return JObject.Parse(text);
        }

        [CanBeNull]
        private static JObject ReadSecretsJsonSafe(IProject project)
        {
            var projectFiles =
                project.GetAllProjectFiles(
                    file => file.Name.Equals(FileNames.NetCoreProjectJson, StringComparison.OrdinalIgnoreCase));

            foreach (var projectFile in projectFiles)
            {
                try
                {
                    var json = ParseJsonProjectFile(projectFile);
                    var userSecretsId = json[SettingsConstants.NetCoreUserSecretsIdJsonProperty];

                    if (userSecretsId != null && userSecretsId.Type == JTokenType.String)
                    {
                        var filePath = string.Format(
                            FileNames.UserSecretsPathFormat,
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            userSecretsId.Value<string>());

                        var secretsFile = File.ReadAllText(filePath);
                        return JObject.Parse(secretsFile);
                    }
                }
                // ReSharper disable once EmptyGeneralCatchClause
                // ReSharper disable once CatchAllClause
                catch (Exception)
                {
                }
            }

            return null;
        }
    }
}
