var target = Argument("target", "Default");
var buildConfiguration = Argument("buildConfig", "Debug");
var extensionsVersion = Argument("version", "2017.2.1");
var waveVersion = Argument("wave", "[9.0]");

var projectName = "Resharper.ConfigurationSense";
var solutionFile = string.Format("./src/{0}.sln", projectName);

Task("AppendBuildNumber")
  .WithCriteria(BuildSystem.AppVeyor.IsRunningOnAppVeyor)
  .Does(() =>
{
	var buildNumber = BuildSystem.AppVeyor.Environment.Build.Number;
	extensionsVersion = string.Format("{0}.{1}", extensionsVersion, buildNumber);
});

Task("UpdateBuildVersion")
  .IsDependentOn("AppendBuildNumber")
  .WithCriteria(BuildSystem.AppVeyor.IsRunningOnAppVeyor)
  .Does(() =>
{
	BuildSystem.AppVeyor.UpdateBuildVersion(extensionsVersion);
});

Task("NugetRestore")
  .Does(() =>
{
	NuGetRestore(solutionFile);
});

Task("UpdateAssemblyVersion")
  .IsDependentOn("AppendBuildNumber")
  .Does(() =>
{
	var assemblyFile = string.Format("src/{0}/Properties/AssemblyInfo.cs", projectName);

	AssemblyInfoSettings assemblySettings = new AssemblyInfoSettings();
	assemblySettings.Title = "Resharper.ConfigurationSense";
	assemblySettings.FileVersion = extensionsVersion;
	assemblySettings.Version = extensionsVersion;

	CreateAssemblyInfo(assemblyFile, assemblySettings);
});

Task("Build")
  .IsDependentOn("NugetRestore")
  .IsDependentOn("UpdateAssemblyVersion")
  .Does(() =>
{
	MSBuild(solutionFile, new MSBuildSettings {
		Verbosity = Verbosity.Minimal,
		Configuration = buildConfiguration
    });
});


Task("NugetPack")
  .IsDependentOn("AppendBuildNumber")
  .IsDependentOn("Build")
  .Does(() =>
{
	 var buildPath = string.Format("./src/{0}/bin/{1}", projectName, buildConfiguration);

	 var files = new List<NuSpecContent>();
     files.Add(new NuSpecContent {Source = string.Format("{0}/{1}.dll", buildPath, projectName), Target = "dotFiles"});

     if (buildConfiguration == "Debug") 
     {
		files.Add(new NuSpecContent {Source = string.Format("{0}/{1}.pdb", buildPath, projectName), Target = "dotFiles"});
     }

     var nuGetPackSettings   = new NuGetPackSettings {
                                     Id                      = projectName,
                                     Version                 = extensionsVersion,
                                     Title                   = "Configuration Sense",
                                     Authors                 = new[] {"Oleg Shevchenko"},
                                     Owners                  = new[] {"Oleg Shevchenko"},
                                     Description             = "Provides autocomplete and validation for App settings and Connections strings",
                                     ProjectUrl              = new Uri("https://github.com/olsh/resharper-configuration-sense"),
                                     IconUrl                 = new Uri("https://raw.githubusercontent.com/olsh/resharper-configuration-sense/master/images/logo.png"),
                                     LicenseUrl              = new Uri("https://github.com/olsh/resharper-configuration-sense/raw/master/LICENSE"),
                                     Tags                    = new [] {"resharper", "configuration", "autocomplete", "intellisense", "validation", "netcore"},
                                     RequireLicenseAcceptance= false,
                                     Symbols                 = false,
                                     NoPackageAnalysis       = true,
                                     Files                   = files,
                                     OutputDirectory         = ".",
									 Dependencies            = new [] { new NuSpecDependency() { Id = "Wave", Version = waveVersion } },
									 ReleaseNotes            = new [] { "https://github.com/olsh/resharper-configuration-sense/releases" }
                                 };

     NuGetPack(nuGetPackSettings);
});

Task("CreateArtifact")
  .IsDependentOn("UpdateBuildVersion")
  .IsDependentOn("NugetPack")
  .WithCriteria(BuildSystem.AppVeyor.IsRunningOnAppVeyor)
  .Does(() =>
{
	BuildSystem.AppVeyor.UploadArtifact(string.Format("{0}.{1}.nupkg", projectName, extensionsVersion));
});

Task("Default")
	.IsDependentOn("NugetPack");

RunTarget(target);
