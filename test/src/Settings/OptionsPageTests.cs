using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Controls.FileSystem;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.IDE.UI;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.TestFramework;

using NUnit.Framework;

using Resharper.ConfigurationSense.Settings;

namespace Resharper.ConfigurationSense.Tests.Settings
{
    // The page builds a Be control tree in its constructor, and several Be constructors reject nulls
    // at runtime rather than at compile time. Nothing else covers that: the Rider searchable-options
    // pass renders the page without a solution, so it only ever reaches the "open a solution" branch
    [TestNet60]
    public sealed class OptionsPageTests : BaseTestWithSingleProject
    {
        protected override string RelativeTestDataPath => @"Settings";

        [Test]
        public void TestOptionsPageIsBuiltWhenSolutionIsOpen()
        {
            DoTestSolution((lifetime, solution) =>
            {
                var settingsStore = solution.GetComponent<ISettingsStore>()
                    .BindToContextLive(lifetime, ContextRange.ApplicationWide);

                var page = new ConfigurationSenseOptionsPage(
                    lifetime,
                    new OptionsPageContext(),
                    new OptionsSettingsSmartContext(settingsStore, settingsStore),
                    solution.GetComponent<IIconHost>(),
                    solution.GetComponent<IShellLocks>(),
                    solution.GetComponent<ICommonFileDialogs>(),
                    solution);

                Assert.That(page.Content, Is.Not.Null);
            });
        }
    }
}
