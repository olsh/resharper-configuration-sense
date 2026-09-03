using NUnit.Framework;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    public class ConnectionStringsAnalyzerTests : SettingsAnalyzerTestBase
    {
        protected override string SubPath => "ConnectionStrings";

        [Test]
        public void TestExistingConnectionString() => DoNamedTest2("app.config");

        [Test]
        public void TestMissingConnectionString() => DoNamedTest2("app.config");
    }
}
