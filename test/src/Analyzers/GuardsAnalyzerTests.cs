using NUnit.Framework;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    // Every case here uses a key that is absent from both configuration files, so anything
    // other than an empty gold means a guard clause of an analyzer stopped working
    public class GuardsAnalyzerTests : SettingsAnalyzerTestBase
    {
        protected override string SubPath => "Guards";

        [Test]
        public void TestNonLiteralArgument() => DoNamedTest2("app.config", "appsettings.json");

        [Test]
        public void TestInterpolatedStringArgument() => DoNamedTest2("app.config", "appsettings.json");

        [Test]
        public void TestTwoArguments() => DoNamedTest2("app.config", "appsettings.json");

        [Test]
        public void TestUnrelatedIndexer() => DoNamedTest2("app.config", "appsettings.json");

        [Test]
        public void TestUnrelatedMethod() => DoNamedTest2("app.config", "appsettings.json");
    }
}
