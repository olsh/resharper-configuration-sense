using NUnit.Framework;

namespace Resharper.ConfigurationSense.Tests.Completion
{
    public class NetCoreGetSectionCompletionTests : SettingsCompletionTestBase
    {
        protected override string SubPath => "NetCoreGetSection";

        // The intermediate objects are offered here as well, not only the leaf values
        [Test]
        public void TestSectionName() => DoNamedTest2("appsettings.json");
    }
}
