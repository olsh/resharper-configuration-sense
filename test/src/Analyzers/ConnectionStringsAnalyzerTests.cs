using NUnit.Framework;

using static Resharper.ConfigurationSense.Tests.Constants.ConfigurationFiles;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    public class ConnectionStringsAnalyzerTests : SettingsAnalyzerTestBase
    {
        protected override string SubPath => "ConnectionStrings";

        [Test]
        public void TestExistingConnectionString() => DoNamedTest2(AppConfig);

        [Test]
        public void TestMissingConnectionString() => DoNamedTest2(AppConfig);
    }
}
