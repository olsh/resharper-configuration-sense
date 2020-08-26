using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

using Resharper.ConfigurationSense.Highlights;

namespace Resharper.ConfigurationSense.Highlights
{
    [RegisterConfigurableSeverity(SeverityId, null, HighlightingGroupIds.CompilerWarnings,
        "The setting wasn't found in configuration files", "The setting wasn't found in configuration files",
        Severity.WARNING)]
    [ConfigurableSeverityHighlighting(SeverityId, CSharpLanguage.Name, OverlapResolve = OverlapResolveKind.WARNING)]
    public class SettingsNotFoundHighlighting : IHighlighting
    {
        public const string SeverityId = "SettingNotFoundInConfiguration";

        private readonly IArgumentList _argumentList;

        private readonly ICSharpArgumentsOwner _argumentsOwner;

        public SettingsNotFoundHighlighting(
            ICSharpArgumentsOwner argumentsOwner,
            IArgumentList argumentList,
            string key,
            string type)
        {
            _argumentsOwner = argumentsOwner;
            _argumentList = argumentList;
            ToolTip = $"{type} {key} wasn't found in configuration files";
        }

        public string ErrorStripeToolTip => ToolTip;

        public string ToolTip { get; }

        public DocumentRange CalculateRange()
        {
            return _argumentList.GetHighlightingRange();
        }

        public bool IsValid()
        {
            if (_argumentsOwner != null)
            {
                return _argumentsOwner.IsValid();
            }

            return true;
        }
    }
}
