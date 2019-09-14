#tool nuget:?package=GitVersion.CommandLine&version=5.0.1
#addin nuget:?package=Cake.Docker&version=0.10.1

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDir = "."; //+ Directory(configuration);

//////////////////////////////////////////////////////////////////////
// Version
//////////////////////////////////////////////////////////////////////

GitVersion versionInfo = null;

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Version")
  .Description("Retrieves the current version from the git repository")
  .Does(() =>
  {

	versionInfo = GitVersion(new GitVersionSettings {
		UpdateAssemblyInfo = false
	});

	Information("Branch: "+ versionInfo.BranchName);
	Information("Version: "+ versionInfo.FullSemVer);
	Information("Version: "+ versionInfo.MajorMinorPatch);

    if(versionInfo.BranchName != "master")
    {
        configuration = Argument("configuration", "Debug");
    }
  });

Task("Clean")
.IsDependentOn("Version")
    .Does(() =>
{
    CleanDirectories(buildDir + "/src/**/bin/" + configuration);
    CleanDirectories(buildDir + "/tests/**/bin/" + configuration);
    CleanDirectories(buildDir + "/src/**/obj/" + configuration);
    CleanDirectories(buildDir + "/tests/**/obj/" + configuration);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore(buildDir + "/simpleauth.sln");
    DotNetCoreBuildServerShutdown();
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    var buildVersion = versionInfo.FullSemVer;
    var informationalVersion = versionInfo.MajorMinorPatch + "." + versionInfo.CommitsSinceVersionSourcePadded;
    if(versionInfo.BranchName == "master")
    {
        buildVersion = informationalVersion;
    }
    var buildSettings = new DotNetCoreMSBuildSettings()
        .SetConfiguration(configuration)
        .SetVersion(buildVersion)
        .SetInformationalVersion(informationalVersion);
        //.SetFileVersion(versionInfo.SemVer + versionInfo.Sha);
    DotNetCoreMSBuild(buildDir + "/simpleauth.sln", buildSettings);
    DotNetCoreBuildServerShutdown();
});

Task("Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    var projects = GetFiles(buildDir + "/tests/**/*.tests.csproj");
    //projects.Add(new FilePath("./tests/simpleauth.acceptancetests/simpleauth.acceptancetests.csproj"));

    foreach(var project in projects)
    {
        Information("Testing: " + project.FullPath);
        var reportName = buildDir + "/artifacts/testreports/" + versionInfo.FullSemVer + "_" + System.IO.Path.GetFileNameWithoutExtension(project.FullPath).Replace('.', '_') + ".xml";
        reportName = System.IO.Path.GetFullPath(reportName);

        Information(reportName);

        var coreTestSettings = new DotNetCoreTestSettings()
          {
			NoBuild = true,
			NoRestore = true,
            // Set configuration as passed by command line
            Configuration = configuration,
            ArgumentCustomization = x => x.Append("--logger \"trx;LogFileName=" + reportName + "\"")
          };

          DotNetCoreTest(
          project.FullPath,
          coreTestSettings);

          DotNetCoreBuildServerShutdown();
    }
});

Task("Pack")
    .IsDependentOn("Tests")
    .Does(()=>
    {
        var nugetVersion = versionInfo.MajorMinorPatch + "-" + versionInfo.BranchName + versionInfo.CommitsSinceVersionSourcePadded;
        Information(versionInfo.CommitsSinceVersionSourcePadded);
        if(versionInfo.BranchName == "master")
        {
            nugetVersion = versionInfo.MajorMinorPatch + "." + versionInfo.CommitsSinceVersionSourcePadded;
        }

        Information("Package version: " + nugetVersion);

        var packSettings = new DotNetCorePackSettings
        {
            Configuration = configuration,
            NoBuild = true,
            NoRestore = true,
            OutputDirectory = "./artifacts/packages",
            IncludeSymbols = true,
            MSBuildSettings = new DotNetCoreMSBuildSettings().SetConfiguration(configuration).SetVersion(nugetVersion)
        };

        DotNetCorePack("./src/simpleauth.shared/simpleauth.shared.csproj", packSettings);
        DotNetCorePack("./src/simpleauth/simpleauth.csproj", packSettings);
        DotNetCorePack("./src/simpleauth.client/simpleauth.client.csproj", packSettings);
        DotNetCorePack("./src/simpleauth.manager.client/simpleauth.manager.client.csproj", packSettings);
        DotNetCorePack("./src/simpleauth.stores.marten/simpleauth.stores.marten.csproj", packSettings);
        DotNetCorePack("./src/simpleauth.sms/simpleauth.sms.csproj", packSettings);
    });

// the rest of your build script
Task("Docker-Build")
.IsDependentOn("Pack")
.Does(() => {
	var publishSettings = new DotNetCorePublishSettings
    {
        Configuration = configuration,
        OutputDirectory = "./artifacts/publish/inmemory/"
    };

    DotNetCorePublish("./src/simpleauth.authserver/simpleauth.authserver.csproj", publishSettings);
    var settings = new DockerImageBuildSettings {
        Compress = true,
        File = "./DockerfileInMemory",
        ForceRm = true,
        Rm = true,
		Tag = new[] {
			"jjrdk/simpleauth:inmemory",
			"jjrdk/simpleauth:" + versionInfo.MajorMinorPatch + "." + versionInfo.CommitsSinceVersionSourcePadded + "-inmemory"
		}
	};
    DockerBuild(settings, "./");

	publishSettings = new DotNetCorePublishSettings
    {
        Configuration = configuration,
        OutputDirectory = "./artifacts/publish/postgres/"
    };

    DotNetCorePublish("./src/simpleauth.authserverpg/simpleauth.authserverpg.csproj", publishSettings);
    settings = new DockerImageBuildSettings {
        Compress = true,
        File = "./DockerfilePostgres",
        ForceRm = true,
        Rm = true,
		Tag = new[] {
			"jjrdk/simpleauth:postgres",
			"jjrdk/simpleauth:" + versionInfo.MajorMinorPatch + "." + versionInfo.CommitsSinceVersionSourcePadded + "-postgres"
		}
	};
    DockerBuild(settings, "./");
});

Task("Warp")
    .IsDependentOn("Pack")
    .Does(()=>
    {
        DotNetCoreTool(buildDir + "/src/simpleauth.authserver/simpleauth.authserver.csproj", "warp", "-l aggressive -v");

        var packfolder = "linux-x64";
        if (IsRunningOnWindows())
        {
            Information("Publishing for Windows x64");
            packfolder = "win-x64";
        }
        else
        {
            Information("Publishing for Linux x64");
        }
        var outputFolder = buildDir + "/artifacts/authserver/" + packfolder;
        EnsureDirectoryExists(outputFolder);
        CopyDirectory(Directory(buildDir + "/src/simpleauth.authserver/bin/Release/netcoreapp2.1/" + packfolder), outputFolder);
        DeleteFiles(outputFolder + "/*.pdb");
        DeleteFiles(outputFolder + "/*.xml");
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Docker-Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
