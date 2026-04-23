using System;
using System.IO;
using System.Linq;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace CakeBuild;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public static readonly string[] ProjectNames =
        { "TranslocatorLocatorCmdMod", "TranslocatorNavigatorMod", "WaterSpongeMod", "BlueprintCopy" };

    public BuildContext(ICakeContext context)
        : base(context)
    {
        BuildConfiguration = context.Argument("configuration", "Release");
        SkipJsonValidation = context.Argument("skipJsonValidation", false);
    }

    public string BuildConfiguration { get; }
    public bool SkipJsonValidation { get; }
}

[TaskName("ValidateJson")]
public sealed class ValidateJsonTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (context.SkipJsonValidation) return;
        foreach (var projectName in BuildContext.ProjectNames)
        {
            var jsonFiles = context.GetFiles($"../{projectName}/assets/**/*.json");
            foreach (var file in jsonFiles)
                try
                {
                    var json = File.ReadAllText(file.FullPath);
                    JToken.Parse(json);
                }
                catch (JsonException ex)
                {
                    throw new Exception(
                        $"Validation failed for JSON file: {file.FullPath}{Environment.NewLine}{ex.Message}", ex);
                }
        }
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(ValidateJsonTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var toolPath = Path.Combine(homePath, ".dotnet", "dotnet");

        foreach (var projectName in BuildContext.ProjectNames)
        {
            var csproj = $"../{projectName}/{projectName}.csproj";
            if (!context.FileExists(csproj)) 
            {
                context.Log.Information($"{projectName} is a content-only mod. Skipping dotnet build.");
                continue;
            }
            
            var publishDir = $"../{projectName}/bin/{context.BuildConfiguration}/Mods/mod/publish";

            // CRITICAL: Clean the internal publish folder to prevent "ghost" DLLs
            if (context.DirectoryExists(publishDir)) context.CleanDirectory(publishDir);

            context.DotNetClean(csproj,
                new DotNetCleanSettings
                {
                    ToolPath = toolPath,
                    Configuration = context.BuildConfiguration
                });

            context.DotNetPublish(csproj,
                new DotNetPublishSettings
                {
                    ToolPath = toolPath,
                    Configuration = context.BuildConfiguration
                });
        }
    }
}

[TaskName("Package")]
[IsDependentOn(typeof(BuildTask))]
public sealed class PackageTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.EnsureDirectoryExists("../Releases");
        context.CleanDirectory("../Releases");

        foreach (var projectName in BuildContext.ProjectNames)
        {
            // We load the modinfo dynamically inside the loop for each project
            var modInfoPath = $"../{projectName}/modinfo.json";
            var modInfo = context.DeserializeJsonFromFile<ModInfo>(modInfoPath);

            // Use ModID or Name for the folder to keep it unique
            var modFolderName = modInfo.ModID ?? projectName;
            var outputDir = $"../Releases/{modFolderName}";

            // Extract the Game Version from dependencies
            // Defaults to "unknown" if the 'game' key is missing
            string vsVersion = "any";
            if (modInfo.Dependencies != null)
            {
                var gameMod = modInfo.Dependencies.FirstOrDefault(d => d.ModID == "game");
                if (gameMod != null)
                {
                    vsVersion = gameMod.Version;
                }
            }
            
            context.EnsureDirectoryExists(outputDir);

            // COPY BINARIES
            context.CopyFiles($"../{projectName}/bin/{context.BuildConfiguration}/Mods/mod/publish/*",
                outputDir);

            // COPY ASSETS
            if (context.DirectoryExists($"../{projectName}/assets"))
                context.CopyDirectory($"../{projectName}/assets", $"{outputDir}/assets");

            // COPY METADATA
            context.CopyFile(modInfoPath, $"{outputDir}/modinfo.json");
            if (context.FileExists($"../{projectName}/modicon.png"))
                context.CopyFile($"../{projectName}/modicon.png", $"{outputDir}/modicon.png");

            var zipFileName = $"../Releases/{modFolderName}_{vsVersion}_{modInfo.Version}.zip";
            
            // ZIP INDIVIDUALLY
            context.Zip(outputDir, zipFileName);

            context.DeleteDirectory(outputDir, new DeleteDirectorySettings {
                Recursive = true,
                Force = true
            });
            
            context.Log.Information($"Successfully packaged: {modFolderName}");
        }
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(PackageTask))]
public class DefaultTask : FrostingTask
{
}