// See https://aka.ms/new-console-template for more information

using System.ComponentModel;
using Newtonsoft.Json;
using Spectre.Console.Cli;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.SchematicCli;

public class SearchSchematicsCommand : Command<SearchSchematicsCommand.Settings>
{
    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var request = new Request(settings.Domain, settings.PartialPath, settings.TreeKey, settings.TreeValue);
        foreach (var path in settings.VintagestoryPaths) Run(path, request);
        return 0;
    }

    private static List<Result> ProcessToResults(BlockSchematic data, Request request)
    {
        var results = data.Indices.Zip(data.BlockIds, (index, blockId) =>
        {
            data.BlockCodes.TryGetValue(blockId, out var assetLocation);
            return new Result(index, blockId, assetLocation, null, null);
        }).Where(result => result.AssetLocation != null &&
                           result.AssetLocation.BeginsWith(request.Domain, request.PartialPath));

        if (request.TreeKey != null)
            results = results.Select(result =>
            {
                if (!data.BlockEntities.TryGetValue(result.Index, out var rawBlockEntity)) return result;
                var beBytes = Ascii85.Decode(rawBlockEntity);
                if (beBytes == null) return result;
                using var ms = new MemoryStream(beBytes);
                var reader = new BinaryReader(ms);
                var tree = new TreeAttribute();
                tree.FromBytes(reader);

                var treeValue = tree.GetAsString(request.TreeKey);
                if (treeValue == null) return result;

                return result with { TreeKey = request.TreeKey, TreeValue = treeValue };
            }); //.Where(result => request.TreeKey == result.TreeKey && request.TreeValue == result.TreeValue);

        return results.ToList();
    }

    private static IOrderedEnumerable<AggregateResult> ProcessToAggregateResults(List<Result> results)
    {
        return results.GroupBy(result => new AggregateKey(result.AssetLocation, result.TreeKey, result.TreeValue))
            .Select(group =>
            {
                var count = group.Count();

                return new AggregateResult(group.Key.AssetLocation, group.Key.TreeKey, group.Key.TreeValue, count);
            }).OrderBy(result => result.Count);
    }

    private static void ProcessFileContent(string filePath, string jsonContent, Request request)
    {
        // Deserialize into your BlockSchematic class
        var data = JsonConvert.DeserializeObject<BlockSchematic>(jsonContent);
        if (data == null) return;

        var results = ProcessToResults(data, request);
        var aggregates = ProcessToAggregateResults(results);

        foreach (var result in aggregates)
        {
            var output = $"{filePath}: block = {result.AssetLocation}, count = {result.Count}";
            if (result is { TreeKey: not null, TreeValue: not null })
                output += $", treeKey={result.TreeKey}, treeValue={result.TreeValue}";
            Console.WriteLine(output);
        }
    }

    private static void ProcessFilePath(string filePath, Request request)
    {
        // Read the file content
        var jsonContent = File.ReadAllText(filePath);
        ProcessFileContent(filePath, jsonContent, request);
    }

    private static void ProcessModPath(string modPath, Request request)
    {
        if (!Directory.Exists(modPath))
        {
            Console.WriteLine($"Failed to parse: {modPath}");
            return;
        }

        // Define the root path (Mod/assets/game/worldgen/schematics)
        var schematicsPath = Path.Combine(modPath, "worldgen", "schematics");

        if (Directory.Exists(schematicsPath))
            Console.WriteLine($"Parsing Schematics at {schematicsPath}");
        else
            return;

        // SearchOption.AllDirectories mimics Python's .rglob('*.json')
        var files = Directory.GetFiles(schematicsPath, "*.json", SearchOption.AllDirectories);

        foreach (var filePath in files)
            try
            {
                ProcessFilePath(filePath, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading {filePath}: {ex.Message}");
            }
    }

    private static void Run(string basePath, Request request)
    {
        var assetPath = Path.Combine(basePath, "assets");

        if (Directory.Exists(assetPath))
        {
            Console.WriteLine($"Parsing {assetPath}");
        }
        else
        {
            Console.WriteLine($"Failed to parse: {assetPath}");
            return;
        }

        foreach (var modPath in Directory.EnumerateDirectories(assetPath)) ProcessModPath(modPath, request);
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[vintagestoryPath]")]
        [Description("Path to the Vintagestory")]
        public required string[] VintagestoryPaths { get; init; }

        [CommandOption("-d|--domain")]
        [Description("The domain to add")]
        [DefaultValue("game")]
        public required string Domain { get; init; }

        [CommandOption("-p|--partialPath")]
        [Description("The partial path to add")]
        [DefaultValue("tapestry")]
        public required string PartialPath { get; init; }

        [CommandOption("-k|--treeKey")]
        [Description("The Tree Key to add")]
        [DefaultValue("type")]
        public required string TreeKey { get; init; }

        [CommandOption("-v|--treeValue")]
        [Description("The partial path to add")]
        public string? TreeValue { get; init; }
    }
}