using NUnit.Framework;

using static Resharper.ConfigurationSense.Tests.Constants.ConfigurationFiles;

namespace Resharper.ConfigurationSense.Tests.Analyzers
{
    // Every case here uses a key that is absent from both configuration files, so anything
    // other than an empty gold means a guard clause of an analyzer stopped working
    public class GuardsAnalyzerTests : SettingsAnalyzerTestBase
    {
        protected override string SubPath => "Guards";

        [Test]
        public void TestNonLiteralArgument() => DoNamedTest2(AppConfig, AppSettingsJson);

        [Test]
        public void TestInterpolatedStringArgument() => DoNamedTest2(AppConfig, AppSettingsJson);

        [Test]
        public void TestTwoArguments() => DoNamedTest2(AppConfig, AppSettingsJson);

        [Test]
        public void TestUnrelatedIndexer() => DoNamedTest2(AppConfig, AppSettingsJson);

        [Test]
        public void TestUnrelatedMethod() => DoNamedTest2(AppConfig, AppSettingsJson);
    }
}
