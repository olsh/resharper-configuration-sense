using NUnit.Framework;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    public class ConfigSourceAnalyzerTests : SettingsAnalyzerTestBase
    {
        protected override string SubPath => "ConfigSource";

        [Test]
        public void TestSettingFromExternalConfigSource() => DoNamedTest2("web.config", "external.config");

        [Test]
        public void TestMissingSettingWithExternalConfigSource() => DoNamedTest2("web.config", "external.config");
    }
}
