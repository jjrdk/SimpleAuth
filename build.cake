#tool nuget:?package=GitVersion.CommandLine&version=5.3.5
#addin nuget:?package=Cake.Docker&version=0.11.0

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
string buildVersion = "";

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

    buildVersion = versionInfo.MajorMinorPatch + "-" + versionInfo.BranchName.Replace("features/", "") + "." + versionInfo.CommitsSinceVersionSource;
	if(versionInfo.BranchName == "master")
    {
        buildVersion = versionInfo.MajorMinorPatch;
    }
    else
    {
        configuration = Argument("configuration", "Debug");
    }

    Information("Build configuration: " + configuration);
	Information("Branch: " + versionInfo.BranchName);
	Information("Version: " + versionInfo.FullSemVer);
	Information("Version: " + versionInfo.MajorMinorPatch);
    Information("Build version: " + buildVersion);
    Information("CommitsSinceVersionSourcePadded: " + versionInfo.CommitsSinceVersionSourcePadded);
  });

Task("Clean")
.IsDependentOn("Version")
    .Does(() =>
{
    Information("Clean bin folders");
    CleanDirectories(buildDir + "/src/**/bin/" + configuration);
    CleanDirectories(buildDir + "/tests/**/bin/" + configuration);

    Information("Clean obj folders");
    CleanDirectories(buildDir + "/src/**/obj/" + configuration);
    CleanDirectories(buildDir + "/tests/**/obj/" + configuration);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore(buildDir + "/simpleauth.sln");
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    var informationalVersion = versionInfo.MajorMinorPatch + "." + versionInfo.CommitsSinceVersionSourcePadded;
	var buildSettings = new DotNetCoreMSBuildSettings()
        .SetConfiguration(configuration)
        .SetVersion(buildVersion)
        .SetInformationalVersion(informationalVersion);
        //.SetFileVersion(versionInfo.SemVer + versionInfo.Sha);
    DotNetCoreMSBuild(buildDir + "/simpleauth.sln", buildSettings);
});

Task("Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    var projects = GetFiles(buildDir + "/tests/**/*.tests.csproj");
    projects.Add(new FilePath("./tests/simpleauth.acceptancetests/simpleauth.acceptancetests.csproj"));

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
    }
});

Task("Postgres")
    .IsDependentOn("Tests")
    .Does(() =>
    {
        try
        {
            Information("Docker compose up");

            var upsettings = new DockerComposeUpSettings
            {
                DetachedMode = true,
                Files = new string[] { "./tests/simpleauth.stores.marten.acceptancetests/docker-compose.yml" }
            };
            DockerComposeUp(upsettings);

            var project = new FilePath("./tests/simpleauth.stores.marten.acceptancetests/simpleauth.stores.marten.acceptancetests.csproj");
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

            DotNetCoreTest(project.FullPath, coreTestSettings);
        }
        finally
        {
            Information("Docker compose down");

            var downsettings = new DockerComposeDownSettings
            {
                Files = new string[] { "./tests/simpleauth.stores.marten.acceptancetests/docker-compose.yml" }
            };
            DockerComposeDown(downsettings);
        }
    });


Task("Postgres_Redis")
    .IsDependentOn("Postgres")
    .Does(() =>
    {
        try
        {
            Information("Docker compose up");

            var upsettings = new DockerComposeUpSettings
            {
                DetachedMode = true,
                Files = new string[] { "./tests/simpleauth.stores.redis.acceptancetests/docker-compose.yml" }
            };
            DockerComposeUp(upsettings);

            var project = new FilePath("./tests/simpleauth.stores.redis.acceptancetests/simpleauth.stores.redis.acceptancetests.csproj");
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

            DotNetCoreTest(project.FullPath, coreTestSettings);
        }
        finally
        {
            Information("Docker compose down");

            var downsettings = new DockerComposeDownSettings
            {
                Files = new string[] { "./tests/simpleauth.stores.redis.acceptancetests/docker-compose.yml" }
            };
            DockerComposeDown(downsettings);
        }
    });

Task("Pack")
    .IsDependentOn("Postgres_Redis")
    .Does(()=>
    {
        Information("Package version: " + buildVersion);

        var packSettings = new DotNetCorePackSettings
        {
            Configuration = configuration,
            NoBuild = true,
            NoRestore = true,
            OutputDirectory = "./artifacts/packages",
            IncludeSymbols = true,
            MSBuildSettings = new DotNetCoreMSBuildSettings().SetConfiguration(configuration).SetVersion(buildVersion)
        };

        DotNetCorePack("./src/simpleauth.shared/simpleauth.shared.csproj", packSettings);
        DotNetCorePack("./src/simpleauth/simpleauth.csproj", packSettings);
        DotNetCorePack("./src/simpleauth.client/simpleauth.client.csproj", packSettings);
        DotNetCorePack("./src/simpleauth.stores.marten/simpleauth.stores.marten.csproj", packSettings);
        DotNetCorePack("./src/simpleauth.stores.redis/simpleauth.stores.redis.csproj", packSettings);
        DotNetCorePack("./src/simpleauth.sms/simpleauth.sms.csproj", packSettings);
        DotNetCorePack("./src/simpleauth.ui/simpleauth.ui.csproj", packSettings);
        DotNetCorePack("./src/simpleauth.sms.ui/simpleauth.sms.ui.csproj", packSettings);
    });

// the rest of your build script
Task("Docker-Build")
.IsDependentOn("Pack")
.Does(() => {

	var winPublishSettings = new DotNetCorePublishSettings
    {
        PublishTrimmed = true,
        Runtime = "win-x64",
        SelfContained = true,
        Configuration = configuration,
        OutputDirectory = "./artifacts/publish/winx64/"
    };

    DotNetCorePublish("./src/simpleauth.authserver/simpleauth.authserver.csproj", winPublishSettings);
	var publishSettings = new DotNetCorePublishSettings
    {
        PublishTrimmed = true,
        Runtime = "linux-musl-x64",
        SelfContained = true,
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
			"jjrdk/simpleauth:inmemory-canary",
			"jjrdk/simpleauth:" + buildVersion + "-inmemory"
		}
	};
    DockerBuild(settings, "./");

	publishSettings.OutputDirectory = "./artifacts/publish/postgres/";
    
    DotNetCorePublish("./src/simpleauth.authserverpg/simpleauth.authserverpg.csproj", publishSettings);
    settings = new DockerImageBuildSettings {
        Compress = true,
        File = "./DockerfilePostgres",
        ForceRm = true,
        Rm = true,
		Tag = new[] {
			"jjrdk/simpleauth:postgres-canary",
			"jjrdk/simpleauth:" + buildVersion + "-postgres"
		}
	};
    DockerBuild(settings, "./");
    
	publishSettings.OutputDirectory = "./artifacts/publish/pgredis/";
    
    DotNetCorePublish("./src/simpleauth.authserverpgredis/simpleauth.authserverpgredis.csproj", publishSettings);
    settings = new DockerImageBuildSettings {
        Compress = true,
        File = "./DockerfilePgRedis",
        ForceRm = true,
        Rm = true,
		Tag = new[] {
			"jjrdk/simpleauth:pgredis-canary",
			"jjrdk/simpleauth:" + buildVersion + "-pgredis"
		}
	};
    DockerBuild(settings, "./");
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
