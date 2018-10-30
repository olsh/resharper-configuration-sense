using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;

using Newtonsoft.Json.Linq;

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

            var settings = new Dictionary<string, HashSet<string>>();
            foreach (var projectFile in configFiles)
            {
                var json = ParseJsonProjectFile(projectFile);

                var properties = new Queue<JProperty>();
                properties.EnqueueRange(json.Properties());

                var secretsJson = ReadSecretsSafe(project);
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

                    if ((property.Value.Type != JTokenType.Object && property.Value.Type != JTokenType.Array
                             && settingType == JsonSettingType.Value) || settingType == JsonSettingType.All)
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

                    if (!settings.TryGetValue(formattedPath, out var settingValues))
                    {
                        settingValues = new HashSet<string>();
                        settings.Add(formattedPath, settingValues);
                    }

                    settingValues.Add(property.Value.ToString());
                }
            }

            return settings.Select(x => new KeyValueSetting(x.Key, string.Join(", ", x.Value)));
        }

        public static IEnumerable<KeyValueSetting> GetXmlProjectSettings(
            this IProjectFolder project,
            string settingsTagName,
            string settingsKeyAttribute,
            string settingsValueAttributes)
        {
            var result = new Dictionary<string, HashSet<string>>();

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
                    foreach (var setting in settings)
                    {
                        if (!result.TryGetValue(setting.Key, out var settingValues))
                        {
                            settingValues = new HashSet<string>();
                            result.Add(setting.Key, settingValues);
                        }

                        settingValues.Add(setting.Value);
                    }
                }
            }

            return result.Select(x => new KeyValueSetting(x.Key, string.Join(", ", x.Value)));
        }

        private static string FormatJsonPath(JToken property)
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

        private static IEnumerable<IProjectFile> GetXmlConfigFiles(IProjectFolder project)
        {
            var additionalConfigurationFiles = project.GetSolution().GetAdditionalConfigurationFiles();

            var xmlConfigFiles = new HashSet<IProjectFile>(
                project.GetAllProjectFiles(
                    file => file.LanguageType.Is<XmlProjectFileType>()
                            && (file.Name.Equals(FileNames.WebConfig, StringComparison.OrdinalIgnoreCase)
                                || file.Name.Equals(FileNames.AppConfig, StringComparison.OrdinalIgnoreCase)
                                || additionalConfigurationFiles.Contains(file.GetPersistentID()))));

            UnionWithDependentFiles(xmlConfigFiles);

            return xmlConfigFiles;
        }

        private static IEnumerable<IProjectFile> GetNetCoreJsonConfigFiles(IProjectFolder project)
        {
            var additionalConfigurationFiles = project.GetSolution().GetAdditionalConfigurationFiles();

            var netCoreJsonConfigFiles = new HashSet<IProjectFile>(
                project.GetAllProjectFiles(
                    file => file.LanguageType.Is<JsonProjectFileType>()
                            && (file.Name.Equals(FileNames.NetCoreAppSettingsJson, StringComparison.OrdinalIgnoreCase)
                                || additionalConfigurationFiles.Contains(file.GetPersistentID()))));

            UnionWithDependentFiles(netCoreJsonConfigFiles);

            return netCoreJsonConfigFiles;
        }

        private static JObject ParseJsonProjectFile(IProjectFile projectFile)
        {
            var primaryPsiFile = projectFile.GetPrimaryPsiFile();
            var text = primaryPsiFile.GetUnquotedText();
            return JObject.Parse(text);
        }

        private static string GetUserSecretId(this IProject project)
        {
            var xmlFile = project.ProjectFile?.GetPrimaryPsiFile() as IXmlFile;
            var enumerable = xmlFile?.FindAllByTagName("UserSecretsId");
            if (enumerable != null)
            {
                foreach (var xmlTag in enumerable)
                {
                    if (!string.IsNullOrEmpty(xmlTag.InnerValue))
                    {
                        return xmlTag.InnerValue;
                    }
                }
            }

            var projectFiles =
                project.GetAllProjectFiles(
                    file => file.Name.Equals(FileNames.NetCoreProjectJson, StringComparison.OrdinalIgnoreCase));
            foreach (var file in projectFiles)
            {
                var jsonProjectFile = ParseJsonProjectFile(file);

                var jsonToken = jsonProjectFile[SettingsConstants.NetCoreUserSecretsIdJsonProperty];

                if (jsonToken != null && jsonToken.Type == JTokenType.String)
                {
                    return jsonToken.Value<string>();
                }
            }

            return null;
        }

        [CanBeNull]
        private static JObject ReadSecretsSafe(IProject project)
        {
            try
            {
                var userSecretId = project.GetUserSecretId();

                var filePath = string.Format(
                    FileNames.UserSecretsPathFormat,
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    userSecretId);

                var secretsFile = File.ReadAllText(filePath);

                return JObject.Parse(secretsFile);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            // ReSharper disable once CatchAllClause
            catch (Exception)
            {
                // Just swallow the exception
            }

            return null;
        }

        private static void UnionWithDependentFiles(HashSet<IProjectFile> projectFiles)
        {
            projectFiles.UnionWith(projectFiles.SelectMany(p => p.GetDependentFiles()).ToArray());
        }
    }
}
