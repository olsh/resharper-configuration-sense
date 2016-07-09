using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

using Resharper.ConfigSense.Highlights;

[assembly:
    RegisterConfigurableSeverity(SettingsNotFoundHighlighting.SeverityId, null, HighlightingGroupIds.CompilerWarnings,
        "The setting wasn't found in configuration files",
        "The setting wasn't found in configuration files", 
        Severity.WARNING, false)]

namespace Resharper.ConfigSense.Highlights
{
    [ConfigurableSeverityHighlighting(SeverityId, CSharpLanguage.Name, OverlapResolve = OverlapResolveKind.WARNING)]
    public class SettingsNotFoundHighlighting : IHighlighting
    {
        #region Fields

        public const string SeverityId = "SettingNotFoundInConfiguration";

        private readonly IElementAccessExpression _expression;

        #endregion

        #region Constructors

        public SettingsNotFoundHighlighting(IElementAccessExpression expression, string key, string type)
        {
            _expression = expression;
            ToolTip = $"{type} with {key} wasn't found in cofiguration files";
        }

        #endregion

        #region Properties

        public string ErrorStripeToolTip => ToolTip;

        public string ToolTip { get; }

        #endregion

        #region Methods

        public bool IsValid()
        {
            if (_expression != null)
            {
                return _expression.IsValid();
            }

            return true;
        }

        public DocumentRange CalculateRange()
        {
            return _expression.ArgumentList.GetHighlightingRange();
        }

        #endregion
    }
}
