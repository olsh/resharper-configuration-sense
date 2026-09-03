using NUnit.Framework;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    public class NetCoreConfigurationAnalyzerTests : SettingsAnalyzerTestBase
    {
        protected override string SubPath => "NetCoreConfiguration";

        [Test]
        public void TestExistingSettingThroughIndexer() => DoNamedTest2("appsettings.json");

        [Test]
        public void TestMissingSettingThroughIndexer() => DoNamedTest2("appsettings.json");

        [Test]
        public void TestNestedSettingThroughIndexer() => DoNamedTest2("appsettings.json");

        // Only leaf values are offered here, so an object path is reported as missing
        [Test]
        public void TestSectionThroughIndexer() => DoNamedTest2("appsettings.json");

        [Test]
        public void TestExistingSettingThroughGetValue() => DoNamedTest2("appsettings.json");

        [Test]
        public void TestMissingSettingThroughGetValue() => DoNamedTest2("appsettings.json");
    }
}
