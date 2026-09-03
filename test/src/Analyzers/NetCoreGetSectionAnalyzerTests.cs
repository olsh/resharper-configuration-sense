using NUnit.Framework;

using static Resharper.ConfigurationSense.Tests.Constants.ConfigurationFiles;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    public class NetCoreGetSectionAnalyzerTests : SettingsAnalyzerTestBase
    {
        protected override string SubPath => "NetCoreGetSection";

        // Unlike the indexer, GetSection also offers the intermediate objects
        [Test]
        public void TestExistingSection() => DoNamedTest2(AppSettingsJson);

        [Test]
        public void TestMissingSection() => DoNamedTest2(AppSettingsJson);
    }
}
