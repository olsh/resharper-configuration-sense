using NUnit.Framework;

using static Resharper.ConfigurationSense.Tests.Constants.ConfigurationFiles;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    public class NetCoreConfigurationAnalyzerTests : SettingsAnalyzerTestBase
    {
        protected override string SubPath => "NetCoreConfiguration";

        [Test]
        public void TestExistingSettingThroughIndexer() => DoNamedTest2(AppSettingsJson);

        [Test]
        public void TestMissingSettingThroughIndexer() => DoNamedTest2(AppSettingsJson);

        [Test]
        public void TestNestedSettingThroughIndexer() => DoNamedTest2(AppSettingsJson);

        // Only leaf values are offered here, so an object path is reported as missing
        [Test]
        public void TestSectionThroughIndexer() => DoNamedTest2(AppSettingsJson);

        [Test]
        public void TestExistingSettingThroughGetValue() => DoNamedTest2(AppSettingsJson);

        [Test]
        public void TestMissingSettingThroughGetValue() => DoNamedTest2(AppSettingsJson);
    }
}
