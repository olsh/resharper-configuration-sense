using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

using DefaultNamespace;

using NuGet.Versioning;

using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NUnit;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities;
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
        // Read straight from the props file rather than through Solution.GetProperty: evaluating a
        // net472 project pulls in MSBuild, and UpdateSdkVersion runs on a Linux runner
        SdkVersion = XDocument
            .Load((RootDirectory / "Directory.Build.props").ToString())
            .Descendants()
            .Single(x => x.Name.LocalName == "SdkVersion")
            .Value;
        SdkVersion.NotNull("Unable to detect SDK version");

        var versionMatch = Regex.Match(
            SdkVersion,
            @"(?<version>[\d\.]+)(?<suffix>-.*)?",
            RegexOptions.None,
            RegexTimeout);

        SdkVersionWithoutSuffix = versionMatch.Groups["version"]
            .ToString();
        SdkVersionSuffix = versionMatch.Groups["suffix"]
            .ToString();

        // The run number goes before the suffix, so that an EAP build stays a valid prerelease
        ExtensionVersion = GitHubActions == null
            ? SdkVersion
            : $"{SdkVersionWithoutSuffix}.{GitHubActions.RunNumber}{SdkVersionSuffix}";
        var sdkMatch = Regex.Match(SdkVersion, @"\d{2}(\d{2}).(\d).*", RegexOptions.None, RegexTimeout);
        WaveMajorVersion = int.Parse(sdkMatch.Groups[1]
            .Value + sdkMatch.Groups[2]
            .Value);
        WaveVersionsRange = $"{WaveMajorVersion}.0";

        base.OnBuildInitialized();
    }

    [CI] readonly GitHubActions GitHubActions;

    [Parameter] readonly string Configuration = "Release";

    [Parameter] [Secret] readonly string MarketplaceToken;

    [Parameter("Solution to open in the sandboxed IDE")] readonly AbsolutePath RunIdeSolution;

    [Parameter("Adopt this SDK version instead of the one the wave policy picks")] readonly string SdkVersionOverride;

    [Solution(GenerateProjects = true)] readonly Solution Solution;

    [LocalPath("./gradlew.bat")] readonly Tool Gradle;

    // Every regex here runs over a version string of a couple of dozen characters, so the bound is
    // only ever reached by a runaway
    static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    // Every project pins its SDK package to $(SdkVersion), and JetBrains does not always push the
    // four of them at the same minute, so a version counts as available only once all of them have it
    static readonly string[] SdkPackageIds =
    {
        "jetbrains.resharper.sdk",
        "jetbrains.rider.sdk",
        "jetbrains.resharper.sdk.tests",
        "jetbrains.rider.sdk.tests",
    };

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
            // A zero patch is dropped, so 2025.1.0 is 2025.1 there, but 2026.2.1 stays as it is
            var productVersion = SdkVersionWithoutSuffix.EndsWith(".0", StringComparison.Ordinal)
                ? SdkVersionWithoutSuffix.Substring(0, SdkVersionWithoutSuffix.Length - ".0".Length)
                : SdkVersionWithoutSuffix;

            if (!string.IsNullOrEmpty(SdkVersionSuffix))
            {
                // -eap01 is -EAP1 there. The leading zeros go one number at a time, so that -eap10
                // does not collapse onto -EAP1
                var suffix = Regex.Replace(
                    SdkVersionSuffix,
                    @"\d+",
                    x => int.Parse(x.Value)
                        .ToString(),
                    RegexOptions.None,
                    RegexTimeout);
                productVersion += $"{suffix.ToUpperInvariant()}-SNAPSHOT";
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
                // The base path is the compiled output directory, so the logo is passed as an
                // absolute path rather than resolved relative to it
                .AddProperty("logoPath", RootDirectory / "images" / "logo.png")
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
            // The pack targets hand Gradle one interpolated string, so NUKE's argument handler sees
            // the holes and quotes any that contain spaces. This one is assembled in pieces because
            // the solution is optional, which leaves the paths to be quoted here instead
            var outputDirectory = Solution.Resharper_ConfigurationSense_Rider
                .GetOutputDirectory(Configuration)
                .ToString()
                .DoubleQuoteIfNeeded();

            var arguments =
                @$"runIde -PPluginVersion={ExtensionVersion} -PProductVersion={RiderProductVersion} -PDotNetOutputDirectory={outputDirectory} -PDotNetProjectName={Solution.Resharper_ConfigurationSense_Rider.Name}";

            if (RunIdeSolution != null)
            {
                arguments += @$" -PRunIdeSolution={RunIdeSolution.ToString().DoubleQuoteIfNeeded()}";
            }

            Gradle(arguments, logger: RunIdeLogger);
        });

    Target UpdateSdkVersion => _ => _
        .Executes(async () =>
        {
            var availableVersions = await GetPublishedSdkVersions();
            var currentVersion = NuGetVersion.Parse(SdkVersion);

            NuGetVersion targetVersion;
            if (SdkVersionOverride != null)
            {
                var requestedVersion = NuGetVersion.Parse(SdkVersionOverride);
                targetVersion = availableVersions
                    .FirstOrDefault(x => x.Equals(requestedVersion))
                    .NotNull($"{SdkVersionOverride} is not published for every JetBrains SDK package");
            }
            else
            {
                targetVersion = SelectSdkUpdate(currentVersion, availableVersions);
            }

            if (targetVersion == null || targetVersion.Equals(currentVersion))
            {
                Log.Information("The JetBrains SDK {Version} is up to date", SdkVersion);
                PublishGitHubOutput("sdk-update-available", "false");

                return;
            }

            var propsFile = RootDirectory / "Directory.Build.props";
            // A regex rather than XDocument.Save, so that the MSBuild namespace declaration, the
            // attribute order and the comment in the file all survive untouched
            propsFile.WriteAllText(Regex.Replace(
                propsFile.ReadAllText(),
                "<SdkVersion>[^<]*</SdkVersion>",
                $"<SdkVersion>{targetVersion}</SdkVersion>",
                RegexOptions.None,
                RegexTimeout));

            Log.Information("Updated the JetBrains SDK from {Current} to {Target}", SdkVersion, targetVersion);
            ReportSummary(_ => _.AddPair("SDK", $"{SdkVersion} -> {targetVersion}"));

            PublishGitHubOutput("sdk-update-available", "true");
            PublishGitHubOutput("sdk-version", targetVersion.ToString());
            PublishGitHubOutput("previous-sdk-version", SdkVersion);
        });

    static async Task<IReadOnlyCollection<NuGetVersion>> GetPublishedSdkVersions()
    {
        using var client = new HttpClient();

        HashSet<NuGetVersion> versions = null;
        foreach (var packageId in SdkPackageIds)
        {
            var index = await client.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageId}/index.json");

            using var document = JsonDocument.Parse(index);
            var published = document.RootElement
                .GetProperty("versions")
                .EnumerateArray()
                .Select(x => NuGetVersion.Parse(x.GetString()))
                .ToList();

            if (versions == null)
            {
                versions = new HashSet<NuGetVersion>(published, VersionComparer.Default);
            }
            else
            {
                versions.IntersectWith(published);
            }
        }

        return versions;
    }

    // A same wave patch is already covered by the Wave dependency range the package declares, so it
    // is not worth a release; the next wave is, and it shows up as an EAP first. Once the adopted
    // version is a prerelease the whole train is followed instead: eap01 -> rc01 -> the stable release
    static NuGetVersion SelectSdkUpdate(NuGetVersion currentVersion, IEnumerable<NuGetVersion> availableVersions)
    {
        return availableVersions
            .Where(x => x > currentVersion)
            .Where(x => currentVersion.IsPrerelease
                        || x.Major > currentVersion.Major
                        || (x.Major == currentVersion.Major && x.Minor > currentVersion.Minor))
            .OrderBy(x => x)
            .LastOrDefault();
    }

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

    // Hands a value to the later steps of the same job, which is how the SDK update workflow learns
    // what UpdateSdkVersion decided. Outside of GitHub Actions there is nowhere to write it
    static void PublishGitHubOutput(string name, string value)
    {
        var outputFile = (AbsolutePath)GetVariable("GITHUB_OUTPUT");
        outputFile?.AppendAllLines(new[] { $"{name}={value}" });
    }
}
