using System.Text.RegularExpressions;

using DefaultNamespace;

using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AppVeyor;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Tools.SonarScanner;

using Serilog;

using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.SonarScanner.SonarScannerTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;

[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.PackResharper, x => x.PackRider);

    protected override void OnBuildInitialized()
    {
        // Since we use global package management for dependencies
        // We just pick up the first solution
        SdkVersion = Solution.Resharper_ConfigurationSense.GetProperty("SdkVersion");
        SdkVersion.NotNull("Unable to detect SDK version");

        var versionMatch = Regex.Match(SdkVersion, @"(?<version>[\d\.]+)(?<suffix>-.*)?");

        SdkVersionWithoutSuffix = versionMatch.Groups["version"].ToString();
        SdkVersionSuffix = versionMatch.Groups["suffix"].ToString();

        ExtensionVersion = AppVeyor == null ? SdkVersion : $"{SdkVersion}.{AppVeyor.BuildNumber}";
        var sdkMatch = Regex.Match(SdkVersion, @"\d{2}(\d{2}).(\d).*");
        WaveMajorVersion = int.Parse(sdkMatch.Groups[1]
            .Value + sdkMatch.Groups[2]
            .Value);
        WaveVersionsRange = $"{WaveMajorVersion}.0";

        base.OnBuildInitialized();
    }

    [CI] readonly AppVeyor AppVeyor;

    [Parameter] readonly string Configuration = "Release";

    [Parameter("SonarQube API key", Name = "sonar:apikey")] readonly string SonarQubeApiKey;

    [Solution(GenerateProjects = true)] readonly Solution Solution;

    [LocalPath("./gradlew.bat")] readonly Tool Gradle;

    string ExtensionVersion { get; set; }

    string SdkVersion { get; set; }

    string SdkVersionSuffix { get; set; }

    string SdkVersionWithoutSuffix { get; set; }

    string WaveVersionsRange { get; set; }

    int WaveMajorVersion { get; set; }

    Target UpdateBuildVersion => _ => _
        .Requires(() => AppVeyor)
        .Before(Restore)
        .Executes(() =>
        {
            AppVeyor.Instance.UpdateBuildVersion(ExtensionVersion);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s =>
                s.SetProjectFile(Solution.Resharper_ConfigurationSense));
            DotNetRestore(s =>
                s.SetProjectFile(Solution.Resharper_ConfigurationSense_Rider));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetVersionPrefix(ExtensionVersion)
                .EnableNoRestore());
        });

    Target PackResharper => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            NuGetPack(s => s
                .SetTargetPath(BuildProjectDirectory / "Resharper.ConfigurationSense.nuspec")
                .SetVersion(ExtensionVersion)
                .SetBasePath(Solution.Resharper_ConfigurationSense.GetOutputDirectory(Configuration))
                .AddProperty("project", Solution.Resharper_ConfigurationSense.Name)
                .AddProperty("waveVersion", WaveVersionsRange)
                .SetOutputDirectory(RootDirectory));
        });

    Target PackRider => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            // JetBrains is not very consistent in versioning
            // https://github.com/olsh/resharper-structured-logging/issues/35#issuecomment-892764206
            var productVersion = SdkVersionWithoutSuffix.TrimEnd('.', '0');
            if (!string.IsNullOrEmpty(SdkVersionSuffix))
            {
                productVersion += $"{SdkVersionSuffix.Replace("0", string.Empty).ToUpper()}-SNAPSHOT";
            }

            Gradle(@$"buildPlugin -PPluginVersion={ExtensionVersion} -PProductVersion={productVersion} -PDotNetOutputDirectory={Solution.Resharper_ConfigurationSense_Rider.GetOutputDirectory(Configuration)} -PDotNetProjectName={Solution.Resharper_ConfigurationSense_Rider.Name}",
                logger:
                (_, s) =>
                {
                    // Gradle writes warnings to stderr
                    // By default logger will write stderr as errors
                    // AppVeyor writes errors as special messages and stops the build if such messages more than 500
                    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                    Log.Debug(s);
                });
        });

    Target SonarBegin => _ => _
        .Unlisted()
        .Before(Compile)
        .Executes(() =>
        {
            SonarScannerBegin(s => s.SetServer("https://sonarcloud.io")
                .SetFramework("net5.0")
                .SetLogin(SonarQubeApiKey)
                .SetProjectKey("resharper-configuration-sense")
                .SetName("Configuration Sense")
                .SetOrganization("olsh-github")
                .SetVersion("1.0.0.0"));
        });

    Target SonarEnd => _ => _
        .DependsOn(SonarBegin, Compile)
        .Executes(() =>
        {
            SonarScannerEnd(s => s
                .SetLogin(SonarQubeApiKey)
                .SetFramework("net5.0"));
        });
}
