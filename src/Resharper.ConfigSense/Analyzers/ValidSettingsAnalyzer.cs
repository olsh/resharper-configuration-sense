using System.Collections.Generic;

using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

using Resharper.ConfigSense.Constants;
using Resharper.ConfigSense.Extensions;
using Resharper.ConfigSense.Highlights;
using Resharper.ConfigSense.Models;

namespace Resharper.ConfigSense.Analyzers
{
    [ElementProblemAnalyzer(typeof(IElementAccessExpression), 
        HighlightingTypes = new[] { typeof(SettingsNotFoundHighlighting) })]
    public class ValidSettingsAnalyzer : ElementProblemAnalyzer<IElementAccessExpression>
    {
        #region Methods

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

            var accessorClrtype = element.GetAccessorClrType();
            if (string.IsNullOrEmpty(accessorClrtype))
            {
                return;
            }

            var project = element.GetProject();
            if (project == null)
            {
                return;
            }

            IEnumerable<KeyValueSetting> keyValueSettings;
            string type;
            if (accessorClrtype == ClrTypeConstants.AppSettingsClrType)
            {
                keyValueSettings = project.GetProjectSettings(
                    SettingsConstants.AppSettingsTagName, 
                    SettingsConstants.AppSettingsKeyAttribute, 
                    SettingsConstants.AppSettingsValueAttribute);
                type = "App setting";
            }
            else if (accessorClrtype == ClrTypeConstants.ConnectionStringsClrType)
            {
                keyValueSettings = project.GetProjectSettings(
                    SettingsConstants.ConnectionStringTagName, 
                    SettingsConstants.ConnectionStringsKeyAttribute, 
                    SettingsConstants.ConnectionStringsValueAttribute);
                type = "Connection string";
            }
            else
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

            consumer.AddHighlighting(new SettingsNotFoundHighlighting(element, stringValue, type));
        }

        #endregion
    }
}
