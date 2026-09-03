using NUnit.Framework;

using static Resharper.ConfigurationSense.Tests.Constants.ConfigurationFiles;

namespace Resharper.ConfigurationSense.Tests.Completion
{
    public class ConnectionStringsCompletionTests : SettingsCompletionTestBase
    {
        protected override string SubPath => "ConnectionStrings";

        [Test]
        public void TestConnectionStringName() => DoNamedTest2(AppConfig);
    }
}
