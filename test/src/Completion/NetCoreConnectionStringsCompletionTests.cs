using NUnit.Framework;

using static Resharper.ConfigurationSense.Tests.Constants.ConfigurationFiles;

namespace Resharper.ConfigurationSense.Tests.Completion
{
    public class NetCoreConnectionStringsCompletionTests : SettingsCompletionTestBase
    {
        protected override string SubPath => "NetCoreConnectionStrings";

        // The names are offered without the ConnectionStrings: prefix they carry in the file
        [Test]
        public void TestConnectionStringName() => DoNamedTest2(AppSettingsJson);
    }
}
