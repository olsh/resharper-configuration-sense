using NUnit.Framework;

using static Resharper.ConfigurationSense.Tests.Constants.ConfigurationFiles;

namespace Resharper.ConfigurationSense.Tests.Completion
{
    // A key declared in both configuration files produces one item carrying both values
    public class MergedXmlSettingsCompletionTests : SettingsCompletionTestBase
    {
        protected override string SubPath => "MergedXmlSettings";

        [Test]
        public void TestMergedValues() => DoNamedTest2(AppConfig, WebConfig);
    }
}
