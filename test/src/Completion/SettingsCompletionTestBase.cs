using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;

using Resharper.ConfigurationSense.Models;
using Resharper.ConfigurationSense.Tests.Constants;

namespace Resharper.ConfigurationSense.Tests.Completion
{
    [TestNet60]
    [TestPackages(
        NugetPackages.ConfigurationManagerPackage,
        NugetPackages.ConfigurationAbstractionsPackage,
        NugetPackages.ConfigurationBinderPackage,
        Inherits = true)]
    public abstract class SettingsCompletionTestBase : CodeCompletionTestBase
    {
        protected abstract string SubPath { get; }

        protected override string RelativeTestDataPath => @"Completion\" + SubPath;

        protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;

        // The gold files should only cover the items this plugin contributes
        protected override bool LookupItemFilter(ILookupItem lookupItem)
        {
            return lookupItem is KeyValueSettingLookupItem;
        }
    }
}
