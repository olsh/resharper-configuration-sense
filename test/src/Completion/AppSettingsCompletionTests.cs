using NUnit.Framework;

namespace Resharper.ConfigurationSense.Tests.Completion
{
    public class AppSettingsCompletionTests : SettingsCompletionTestBase
    {
        protected override string SubPath => "AppSettings";

        [Test]
        public void TestAppSettingsKey() => DoNamedTest2("app.config");
    }
}
