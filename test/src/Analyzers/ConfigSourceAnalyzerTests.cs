using NUnit.Framework;

using static Resharper.ConfigurationSense.Tests.Constants.ConfigurationFiles;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    public class ConfigSourceAnalyzerTests : SettingsAnalyzerTestBase
    {
        protected override string SubPath => "ConfigSource";

        [Test]
        public void TestSettingFromExternalConfigSource() => DoNamedTest2(WebConfig, "external.config");

        [Test]
        public void TestMissingSettingWithExternalConfigSource() => DoNamedTest2(WebConfig, "external.config");
    }
}
