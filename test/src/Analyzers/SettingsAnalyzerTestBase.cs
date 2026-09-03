using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;

using Resharper.ConfigurationSense.Highlights;
using Resharper.ConfigurationSense.Tests.Constants;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    [TestNet60]
    [TestPackages(
        NugetPackages.ConfigurationManagerPackage,
        NugetPackages.ConfigurationAbstractionsPackage,
        NugetPackages.ConfigurationBinderPackage,
        Inherits = true)]
    public abstract class SettingsAnalyzerTestBase : CSharpHighlightingTestBase
    {
        protected abstract string SubPath { get; }

        protected override string RelativeTestDataPath => @"Analyzers\" + SubPath;

        // Without this every unrelated platform inspection would land in the gold files
        protected override bool HighlightingPredicate(
            IHighlighting highlighting,
            IPsiSourceFile sourceFile,
            IContextBoundSettingsStore settingsStore)
        {
            return highlighting is SettingsNotFoundHighlighting;
        }
    }
}
