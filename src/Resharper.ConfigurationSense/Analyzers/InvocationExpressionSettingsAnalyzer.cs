using System.Collections.Generic;

using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
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
    [ElementProblemAnalyzer(typeof(IInvocationExpression),
         HighlightingTypes = new[] { typeof(SettingsNotFoundHighlighting) })]
    public class InvocationExpressionSettingsAnalyzer : ElementProblemAnalyzer<IInvocationExpression>
    {
        protected override void Run(
            IInvocationExpression element,
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

            var methodPath = element.GetMethodPath();
            if (string.IsNullOrEmpty(methodPath))
            {
                return;
            }

            var project = element.GetProject();
            if (project == null)
            {
                return;
            }

            IEnumerable<KeyValueSetting> keyValueSettings;
            if (methodPath == ClrTypeConstants.NetCoreGetConnectionString)
            {
                keyValueSettings = project.GetJsonProjectSettings(SettingsConstants.NetCoreConnectionStringsJsonPath);
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

            consumer.AddHighlighting(
                new SettingsNotFoundHighlighting(element, element.ArgumentList, stringValue, "Connection string"));
        }
    }
}
