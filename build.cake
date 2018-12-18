var target = Argument("target", "Default");
var buildConfiguration = Argument("buildConfig", "Debug");
var waveVersion = Argument("wave", "[183.0,184.0)");
var host = Argument("Host", "Resharper");

var solutionName = "Resharper.ConfigurationSense";
var projectName = solutionName;
var riderHost = "Rider";
if (host == riderHost)
{
	projectName = solutionName + ".Rider";
}

var solutionFile = string.Format("./src/{0}.sln", solutionName);
var solutionFolder = string.Format("./src/{0}/", solutionName);
var projectFile = string.Format("{0}{1}.csproj", solutionFolder, projectName);

var extensionsVersion = XmlPeek(projectFile, "Project/PropertyGroup[1]/VersionPrefix/text()");

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

Task("Build")
  .Does(() =>
{
	DotNetCoreBuild(projectFile, new DotNetCoreBuildSettings {
		Configuration = buildConfiguration
    });
});


Task("NugetPack")
  .IsDependentOn("AppendBuildNumber")
  .IsDependentOn("Build")
  .Does(() =>
{
	 var buildPath = string.Format("./src/{0}/bin/{1}", solutionName, buildConfiguration);

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

	 if (host == riderHost)
	 {
	     var tempDirectory = "./temp/";
	     if (DirectoryExists(tempDirectory))
		 {
	         DeleteDirectory(tempDirectory, new DeleteDirectorySettings { Force = true, Recursive = true });
		 }

	     var riderMetaFolderName = "rider-configuration-sense";
		 var riderMetaFolderPath = string.Format("{0}{1}/", tempDirectory, riderMetaFolderName);
         CopyDirectory(string.Format("./src/{0}/", riderMetaFolderName), riderMetaFolderPath);
		 var nugetPackage = string.Format("{0}.{1}.nupkg", projectName, extensionsVersion);
		 CopyFile(nugetPackage, string.Format("{0}{1}", riderMetaFolderPath, nugetPackage));

		 XmlPoke(string.Format("{0}META-INF/plugin.xml", riderMetaFolderPath), "idea-plugin/version", extensionsVersion, new XmlPokeSettings { Encoding = new UTF8Encoding(false) });

		 Zip(tempDirectory, string.Format("./{0}.zip", riderMetaFolderName));
	 }
});

Task("CreateArtifact")
  .IsDependentOn("UpdateBuildVersion")
  .IsDependentOn("NugetPack")
  .WithCriteria(BuildSystem.AppVeyor.IsRunningOnAppVeyor)
  .Does(() =>
{
	var artifactFile = string.Format("{0}.{1}.nupkg", projectName, extensionsVersion);
	if (host == riderHost)
	{
		artifactFile = string.Format("rider-configuration-sense.zip");
	}

	BuildSystem.AppVeyor.UploadArtifact(artifactFile);
});

Task("Default")
	.IsDependentOn("NugetPack");

RunTarget(target);
