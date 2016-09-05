namespace Resharper.ConfigurationSense.Constants
{
    public static class ClrTypeConstants
    {
        public const string AppSettingsPath = "System.Configuration.ConfigurationManager.AppSettings";

        public const string ConnectionStringsPath = "System.Configuration.ConfigurationManager.ConnectionStrings";

        public const string NetCoreConfiguration = "Microsoft.Extensions.Configuration.IConfiguration";

        public const string NetCoreGetConnectionString =
            "Microsoft.Extensions.Configuration.ConfigurationExtensions.GetConnectionString";
    }
}
