using NUnit.Framework;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    public class NetCoreGetSectionAnalyzerTests : SettingsAnalyzerTestBase
    {
        protected override string SubPath => "NetCoreGetSection";

        // Unlike the indexer, GetSection also offers the intermediate objects
        [Test]
        public void TestExistingSection() => DoNamedTest2("appsettings.json");

        [Test]
        public void TestMissingSection() => DoNamedTest2("appsettings.json");
    }
}
