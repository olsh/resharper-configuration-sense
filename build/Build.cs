using System.Text.RegularExpressions;

using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AppVeyor;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Tools.SonarScanner;

using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.SonarScanner.SonarScannerTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Pack);

    protected override void OnBuildInitialized()
    {
        SdkVersion = Project.GetProperty("SdkVersion");
        SdkVersion.NotNull("Unable to detect SDK version");

        ExtensionVersion = AppVeyor == null ? SdkVersion : $"{SdkVersion}.{AppVeyor.BuildNumber}";
        var sdkMatch = Regex.Match(SdkVersion, @"\d{2}(\d{2}).(\d).*");
        WaveMajorVersion = int.Parse(sdkMatch.Groups[1]
            .Value + sdkMatch.Groups[2]
            .Value);
        WaveVersionsRange = $"[{WaveMajorVersion}.0, {WaveMajorVersion + 1}.0)";

        base.OnBuildInitialized();
    }

    [CI] readonly AppVeyor AppVeyor;

    [Parameter] readonly string Configuration = "Release";

    [Parameter] readonly bool IsRiderHost;

    [Solution] readonly Solution Solution;

    string NuGetPackageFileName => $"{Project.Name}.{ExtensionVersion}.nupkg";

    string NuGetPackagePath => RootDirectory / NuGetPackageFileName;

    string RiderPackagePath => RootDirectory / "rider-configuration-sense.zip";

    string SonarQubeApiKey => GetVariable<string>("sonar:apikey");

    Project Project => IsRiderHost
        ? Solution.GetProject("Resharper.ConfigurationSense.Rider")
        : Solution.GetProject("Resharper.ConfigurationSense");

    AbsolutePath ProjectFilePath => Project.Path;

    AbsolutePath OutputDirectory => Project.Directory / "bin" / Configuration;

    string ExtensionVersion { get; set; }

    string SdkVersion { get; set; }

    string WaveVersionsRange { get; set; }

    int WaveMajorVersion { get; set; }

    [LocalExecutable("./gradlew.bat")] readonly Tool Gradle;

    Target UpdateBuildVersion => _ => _
        .Requires(() => AppVeyor)
        .Executes(() =>
        {
            AppVeyor.Instance.UpdateBuildVersion(ExtensionVersion);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(ProjectFilePath));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(ProjectFilePath)
                .SetConfiguration(Configuration)
                .SetVersionPrefix(ExtensionVersion)
                .SetOutputDirectory(OutputDirectory)
                .EnableNoRestore());
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            NuGetPack(s => s
                .SetTargetPath(BuildProjectDirectory / "Resharper.ConfigurationSense.nuspec")
                .SetVersion(ExtensionVersion)
                .SetBasePath(OutputDirectory)
                .AddProperty("project", Project.Name)
                .AddProperty("waveVersion", WaveVersionsRange)
                .SetOutputDirectory(RootDirectory));
        });

    Target PackRiderPlugin => _ => _
        .DependsOn(Compile)
        .Requires(() => IsRiderHost)
        .Executes(() =>
        {
            Gradle($"buildPlugin -PPluginVersion={ExtensionVersion} -PProductVersion={SdkVersion} -PDotNetOutputDirectory={OutputDirectory} -PDotNetProjectName={Project.Name}", customLogger:
                (_, s) =>
                {
                    // Gradle writes warnings to stderr
                    // By default logger will write stderr as errors
                    // AppVeyor writes errors as special messages and stops the build if such messages more than 500
                    Logger.Normal(s);
                });

            CopyFile(RootDirectory / "gradle-build" / "distributions" / $"rider-configuration-sense-{ExtensionVersion}.zip", RiderPackagePath, FileExistsPolicy.Overwrite);
        });

    Target SonarBegin => _ => _
        .Unlisted()
        .Before(Compile)
        .Executes(() =>
        {
            SonarScannerBegin(s => s
                .SetServer("https://sonarcloud.io")
                .SetFramework("net5.0")
                .SetLogin(SonarQubeApiKey)
                .SetProjectKey("resharper-configuration-sense")
                .SetName("Configuration Sense")
                .SetOrganization("olsh-github")
                .SetVersion("1.0.0.0"));
        });

    Target Sonar => _ => _
        .DependsOn(SonarBegin, Compile)
        .Requires(() => !IsRiderHost)
        .Executes(() =>
        {
            SonarScannerEnd(s => s
                .SetLogin(SonarQubeApiKey)
                .SetFramework("net5.0"));
        });

    Target UploadReSharperArtifact => _ => _
        .DependsOn(Pack)
        .Requires(() => AppVeyor)
        .Requires(() => !IsRiderHost)
        .Executes(() =>
        {
            AppVeyor.PushArtifact(NuGetPackagePath);
        });

    Target UploadRiderArtifact => _ => _
        .DependsOn(PackRiderPlugin)
        .Requires(() => AppVeyor)
        .Requires(() => IsRiderHost)
        .Executes(() =>
        {
            AppVeyor.PushArtifact(RiderPackagePath);
        });
}
