using NUnit.Framework;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    public class AppSettingsAnalyzerTests : SettingsAnalyzerTestBase
    {
        protected override string SubPath => "AppSettings";

        [Test]
        public void TestExistingAppSetting() => DoNamedTest2("app.config");

        [Test]
        public void TestMissingAppSetting() => DoNamedTest2("app.config");
    }
}
