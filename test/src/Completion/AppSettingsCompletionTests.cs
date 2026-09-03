using NUnit.Framework;

using static Resharper.ConfigurationSense.Tests.Constants.ConfigurationFiles;

namespace Resharper.ConfigurationSense.Tests.Completion
{
    public class AppSettingsCompletionTests : SettingsCompletionTestBase
    {
        protected override string SubPath => "AppSettings";

        [Test]
        public void TestAppSettingsKey() => DoNamedTest2(AppConfig);
    }
}
