using NUnit.Framework;

namespace Resharper.ConfigurationSense.Tests.Completion
{
    public class NetCoreConfigurationCompletionTests : SettingsCompletionTestBase
    {
        protected override string SubPath => "NetCoreConfiguration";

        [Test]
        public void TestIndexerKey() => DoNamedTest2("appsettings.json");

        [Test]
        public void TestGetValueKey() => DoNamedTest2("appsettings.json");
    }
}
