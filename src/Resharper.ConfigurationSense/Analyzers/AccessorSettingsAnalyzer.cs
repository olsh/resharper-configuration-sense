using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

using Resharper.ConfigurationSense.Constants;
using Resharper.ConfigurationSense.Extensions;
using Resharper.ConfigurationSense.Highlights;
using Resharper.ConfigurationSense.Models;

namespace Resharper.ConfigurationSense.Analyzers
{
    [ElementProblemAnalyzer(typeof(IElementAccessExpression),
         HighlightingTypes = new[] { typeof(SettingsNotFoundHighlighting) })]
    public class AccessorSettingsAnalyzer : ElementProblemAnalyzer<IElementAccessExpression>
    {
        // ReSharper disable once CognitiveComplexity
        protected override void Run(
            IElementAccessExpression element,
            ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            var arguments = element.ArgumentList.Arguments;
            if (arguments.Count != 1)
            {
                return;
            }

            var expression = arguments[0]?.Value;
            if (!(expression is ICSharpLiteralExpression))
            {
                return;
            }

            var stringValue = expression.FirstChild?.GetUnquotedText();
            if (stringValue == null)
            {
                return;
            }

            var accessorPath = element.GetAccessorPath();
            var accessorSuperTypes = element.GetAccessorSuperTypes();

            // ReSharper disable once PossibleMultipleEnumeration
            if (string.IsNullOrEmpty(accessorPath) && !accessorSuperTypes.Any())
            {
                return;
            }

            var project = element.GetProject();
            if (project == null)
            {
                return;
            }

            IEnumerable<KeyValueSetting> keyValueSettings = null;
            string type = null;
            if (accessorPath == ClrTypeConstants.AppSettingsPath)
            {
                keyValueSettings = project.GetXmlProjectSettings(
                    SettingsConstants.AppSettingsTagName,
                    SettingsConstants.AppSettingsKeyAttribute,
                    SettingsConstants.AppSettingsValueAttribute);
                type = "App setting";
            }
            else if (accessorPath == ClrTypeConstants.ConnectionStringsPath)
            {
                keyValueSettings = project.GetXmlProjectSettings(
                    SettingsConstants.ConnectionStringTagName,
                    SettingsConstants.ConnectionStringsKeyAttribute,
                    SettingsConstants.ConnectionStringsValueAttribute);
                type = "Connection string";
            }

            // ReSharper disable once PossibleMultipleEnumeration
            if (accessorSuperTypes.Any(t => t.ToString().Equals(ClrTypeConstants.NetCoreConfiguration, StringComparison.OrdinalIgnoreCase)))
            {
                keyValueSettings = project.GetJsonProjectSettings(JsonSettingType.Value);
                type = "Setting";
            }

            if (keyValueSettings == null)
            {
                return;
            }

            foreach (var setting in keyValueSettings)
            {
                if (setting.Key.Equals(stringValue))
                {
                    return;
                }
            }

            consumer.AddHighlighting(new SettingsNotFoundHighlighting(element, element.ArgumentList, stringValue, type));
        }
    }
}
