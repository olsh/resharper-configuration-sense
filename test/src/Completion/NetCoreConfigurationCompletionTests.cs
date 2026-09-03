using NUnit.Framework;

using static Resharper.ConfigurationSense.Tests.Constants.ConfigurationFiles;

namespace Resharper.ConfigurationSense.Tests.Completion
{
    public class NetCoreConfigurationCompletionTests : SettingsCompletionTestBase
    {
        protected override string SubPath => "NetCoreConfiguration";

        [Test]
        public void TestIndexerKey() => DoNamedTest2(AppSettingsJson);

        [Test]
        public void TestGetValueKey() => DoNamedTest2(AppSettingsJson);
    }
}
