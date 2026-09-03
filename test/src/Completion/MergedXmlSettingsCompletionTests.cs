using NUnit.Framework;

namespace Resharper.ConfigurationSense.Tests.Completion
{
    // A key declared in both configuration files produces one item carrying both values
    public class MergedXmlSettingsCompletionTests : SettingsCompletionTestBase
    {
        protected override string SubPath => "MergedXmlSettings";

        [Test]
        public void TestMergedValues() => DoNamedTest2("app.config", "web.config");
    }
}
