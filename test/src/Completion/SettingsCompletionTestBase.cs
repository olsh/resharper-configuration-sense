using System;
using System.IO;
using System.Text.RegularExpressions;

using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Utils;

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
        // Both patterns run over a completion dump of a few dozen lines, so the bound is only ever
        // reached by a runaway
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

        // 2026.3 started printing the prefix char handling rules; 2026.2 has no such line
        private static readonly Regex PrefixRulesLine = new Regex(
            @"^Rules: .*(\r?\n)?",
            RegexOptions.Multiline,
            RegexTimeout);

        // The evaluation-source flags lead the relevance line: 2026.2 reports FromSingleCompletion and
        // FromLightAndDynamicEvaluation where 2026.3 reports FromLightEvaluation. Anchored to the start
        // of that line, so a setting key that happens to begin with From is left alone
        private static readonly Regex EvaluationSourceFlags = new Regex(
            @"(?<=^[ \t]*\[)(From\w+, )+",
            RegexOptions.Multiline,
            RegexTimeout);

        protected abstract string SubPath { get; }

        protected override string RelativeTestDataPath => @"Completion\" + SubPath;

        protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;

        // The gold files should only cover the items this plugin contributes
        protected override bool LookupItemFilter(ILookupItem lookupItem)
        {
            return lookupItem is KeyValueSettingLookupItem;
        }

        // Both lines dropped here describe the completion engine rather than the items this plugin
        // offers, and they are the only part of the dump that differs between waves. Without them one
        // set of gold files holds for the stable wave and the EAP alike, which is what lets a release
        // be built and tested against an overridden SDK
        protected override TestFailureException ExecuteWithGold(IDocument document, Action<TextWriter> action)
        {
            return base.ExecuteWithGold(
                document,
                writer =>
                {
                    using (var dump = new StringWriter())
                    {
                        action(dump);

                        var normalized = PrefixRulesLine.Replace(dump.ToString(), string.Empty);
                        writer.Write(EvaluationSourceFlags.Replace(normalized, string.Empty));
                    }
                });
        }
    }
}
