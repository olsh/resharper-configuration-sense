using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using DefaultNamespace;

using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NUnit;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;

using Serilog;

using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.NUnit.NUnitTasks;
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

        SdkVersionWithoutSuffix = versionMatch.Groups["version"]
            .ToString();
        SdkVersionSuffix = versionMatch.Groups["suffix"]
            .ToString();

        // The run number goes before the suffix, so that an EAP build stays a valid prerelease
        ExtensionVersion = GitHubActions == null
            ? SdkVersion
            : $"{SdkVersionWithoutSuffix}.{GitHubActions.RunNumber}{SdkVersionSuffix}";
        var sdkMatch = Regex.Match(SdkVersion, @"\d{2}(\d{2}).(\d).*");
        WaveMajorVersion = int.Parse(sdkMatch.Groups[1]
            .Value + sdkMatch.Groups[2]
            .Value);
        WaveVersionsRange = $"{WaveMajorVersion}.0";

        base.OnBuildInitialized();
    }

    [CI] readonly GitHubActions GitHubActions;

    [Parameter] readonly string Configuration = "Release";

    [Parameter] [Secret] readonly string MarketplaceToken;

    [Parameter("Solution to open in the sandboxed IDE")]
    readonly AbsolutePath RunIdeSolution;

    [Solution(GenerateProjects = true)] readonly Solution Solution;

    [LocalPath("./gradlew.bat")] readonly Tool Gradle;

    bool ExtensionVersionReported;

    string ExtensionVersion { get; set; }

    string SdkVersion { get; set; }

    string SdkVersionSuffix { get; set; }

    string SdkVersionWithoutSuffix { get; set; }

    string WaveVersionsRange { get; set; }

    int WaveMajorVersion { get; set; }

    AbsolutePath ReSharperPackagePath =>
        RootDirectory / $"{Solution.Resharper_ConfigurationSense.Name}.{ExtensionVersion}.nupkg";

    static AbsolutePath TestResultsDirectory => RootDirectory / "test" / "results";

    // JetBrains is not very consistent in versioning
    // https://github.com/olsh/resharper-structured-logging/issues/35#issuecomment-892764206
    string RiderProductVersion
    {
        get
        {
            var productVersion = SdkVersionWithoutSuffix.TrimEnd('.', '0');
            if (!string.IsNullOrEmpty(SdkVersionSuffix))
            {
                productVersion += $"{SdkVersionSuffix.Replace("0", string.Empty).ToUpper()}-SNAPSHOT";
            }

            return productVersion;
        }
    }

    // EAP builds must not reach the stable channel of the Marketplace
    string PluginChannel => string.IsNullOrEmpty(SdkVersionSuffix) ? "default" : "eap";

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s =>
                s.SetProjectFile(Solution.Resharper_ConfigurationSense));
            DotNetRestore(s =>
                s.SetProjectFile(Solution.Resharper_ConfigurationSense_Rider));
            DotNetRestore(s =>
                s.SetProjectFile(Solution.Resharper_ConfigurationSense_Tests));
            DotNetRestore(s =>
                s.SetProjectFile(Solution.Resharper_ConfigurationSense_Rider_Tests));
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

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            TestResultsDirectory.CreateDirectory();

            RunTests(Solution.Resharper_ConfigurationSense_Tests);
            RunTests(Solution.Resharper_ConfigurationSense_Rider_Tests);
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

            PublishExtensionVersion();
        });

    Target PackRider => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            // Each interpolation hole has to stay space-free: NUKE quotes any hole that contains
            // spaces, which would collapse the properties into a single argument
            Gradle(
                @$"buildPlugin -PPluginVersion={ExtensionVersion} -PProductVersion={RiderProductVersion} -PDotNetOutputDirectory={Solution.Resharper_ConfigurationSense_Rider.GetOutputDirectory(Configuration)} -PDotNetProjectName={Solution.Resharper_ConfigurationSense_Rider.Name}",
                logger: GradleLogger);

            PublishExtensionVersion();
        });

    Target PublishReSharperPlugin => _ => _
        .DependsOn(PackResharper)
        .Requires(() => MarketplaceToken)
        .Executes(() =>
        {
            NuGetPush(s => s
                .SetTargetPath(ReSharperPackagePath)
                .SetSource("https://plugins.jetbrains.com/")
                .SetApiKey(MarketplaceToken));
        });

    Target PublishRiderPlugin => _ => _
        .DependsOn(PackRider)
        .Requires(() => MarketplaceToken)
        .Executes(() =>
        {
            // NUKE logs tool arguments, so the token travels through the environment instead
            // Seeding from Variables is required because this replaces the child process environment
            var environmentVariables = new Dictionary<string, string>(Variables, StringComparer.OrdinalIgnoreCase)
            {
                ["PUBLISH_TOKEN"] = MarketplaceToken,
            };

            Gradle(
                @$"publishPlugin -PPluginVersion={ExtensionVersion} -PProductVersion={RiderProductVersion} -PDotNetOutputDirectory={Solution.Resharper_ConfigurationSense_Rider.GetOutputDirectory(Configuration)} -PDotNetProjectName={Solution.Resharper_ConfigurationSense_Rider.Name} -PPluginChannel={PluginChannel}",
                environmentVariables: environmentVariables,
                logger: GradleLogger);
        });

    // Launches a sandboxed Rider with the plugin installed, for trying the options page and the
    // analyzers by hand - neither has automated coverage. Blocks until the IDE is closed.
    Target RunIde => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var arguments =
                @$"runIde -PPluginVersion={ExtensionVersion} -PProductVersion={RiderProductVersion} -PDotNetOutputDirectory={Solution.Resharper_ConfigurationSense_Rider.GetOutputDirectory(Configuration)} -PDotNetProjectName={Solution.Resharper_ConfigurationSense_Rider.Name}";

            if (RunIdeSolution != null)
            {
                arguments += @$" -PRunIdeSolution={RunIdeSolution}";
            }

            Gradle(arguments, logger: RunIdeLogger);
        });

    // Both test projects share test/src, so each one writes to bin/<project>/<configuration>.
    // Extensions.GetOutputDirectory hardcodes bin/<configuration> and cannot be used here
    AbsolutePath TestOutputDirectory(Project project) =>
        project.Directory / "bin" / project.Name / Configuration;

    // The results directory also keeps TestResult.xml and the NUnit agent logs out of the repository root
    void RunTests(Project project) =>
        NUnit3(s => s
            .SetInputFiles(TestOutputDirectory(project) / $"{project.Name}.dll")
            .SetWorkPath(TestResultsDirectory)
            .SetResults($"{project.Name}.xml"));

    // Gradle writes warnings to stderr, and the default logger reports stderr as build errors
    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
    static void GradleLogger(OutputType type, string text) => Log.Debug(text);

    // Same stderr problem, but the whole point of RunIde is watching the IDE, so keep it visible
    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
    static void RunIdeLogger(OutputType type, string text) => Log.Information(text);

    // Replaces AppVeyor's UpdateBuildVersion, which used to display the extension version on the
    // build page. GitHub Actions evaluates run-name before any step runs, so the version cannot go
    // there; it goes to the job summary instead, and to the workflow environment so that the upload
    // steps can name the artifacts after it.
    void PublishExtensionVersion()
    {
        ReportSummary(_ => _.AddPair("Version", ExtensionVersion));

        if (GitHubActions == null)
        {
            return;
        }

        var environmentFile = (AbsolutePath)GetVariable("GITHUB_ENV");
        environmentFile?.AppendAllLines(new[] { $"EXTENSION_VERSION={ExtensionVersion}" });

        // Both pack targets call this, and a publish run packs a second time, but the version is
        // the same. The variable exported above is visible to every later step of the same job, and
        // the flag covers the two calls within this one, so between them the heading is written once
        if (ExtensionVersionReported || GetVariable("EXTENSION_VERSION") != null)
        {
            return;
        }

        ExtensionVersionReported = true;
        GitHubActions.StepSummaryFile?.AppendAllLines(new[] { $"### Version `{ExtensionVersion}`" });
    }
}
