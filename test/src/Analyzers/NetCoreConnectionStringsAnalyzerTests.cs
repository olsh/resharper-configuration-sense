using NUnit.Framework;

using static Resharper.ConfigurationSense.Tests.Constants.ConfigurationFiles;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    public class NetCoreConnectionStringsAnalyzerTests : SettingsAnalyzerTestBase
    {
        protected override string SubPath => "NetCoreConnectionStrings";

        // The ConnectionStrings: prefix is stripped before the keys are matched
        [Test]
        public void TestExistingConnectionString() => DoNamedTest2(AppSettingsJson);

        [Test]
        public void TestMissingConnectionString() => DoNamedTest2(AppSettingsJson);
    }
}
