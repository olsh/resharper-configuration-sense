using NUnit.Framework;

using static Resharper.ConfigurationSense.Tests.Constants.ConfigurationFiles;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    public class AppSettingsAnalyzerTests : SettingsAnalyzerTestBase
    {
        protected override string SubPath => "AppSettings";

        [Test]
        public void TestExistingAppSetting() => DoNamedTest2(AppConfig);

        [Test]
        public void TestMissingAppSetting() => DoNamedTest2(AppConfig);
    }
}
