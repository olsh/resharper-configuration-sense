using NUnit.Framework;

using static Resharper.ConfigurationSense.Tests.Constants.ConfigurationFiles;

namespace Resharper.ConfigurationSense.Tests.Completion
{
    public class NetCoreGetSectionCompletionTests : SettingsCompletionTestBase
    {
        protected override string SubPath => "NetCoreGetSection";

        // The intermediate objects are offered here as well, not only the leaf values
        [Test]
        public void TestSectionName() => DoNamedTest2(AppSettingsJson);
    }
}
