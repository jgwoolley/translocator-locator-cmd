// See https://aka.ms/new-console-template for more information

using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.SchematicCli;

public static class Program
{

    private static void ProcessFileContent(string filePath, string jsonContent, Request request)
    {
        // Deserialize into your BlockSchematic class
        var data = JsonConvert.DeserializeObject<BlockSchematic>(jsonContent);
        if (data == null) return;

        var results = data.Indices.Zip(data.BlockIds, (index, blockId) =>
        {
            data.BlockCodes.TryGetValue(blockId, out var assetLocation);
            var tree = new TreeAttribute();
            var result = new Result(index, blockId, assetLocation, tree);

            return result;
        }).Where(result => result.AssetLocation != null &&
                           result.AssetLocation.BeginsWith(request.Domain, request.PartialPath));
        
        if (request.TreeKey != null)
        {
            results = results.Select(result =>
            {
                if (!data.BlockEntities.TryGetValue(result.Index, out var rawBlockEntity)) return result;
                var beBytes = Ascii85.Decode(rawBlockEntity);
                if (beBytes == null) return result;
                using var ms = new MemoryStream(beBytes);
                var reader = new BinaryReader(ms);
                result.Tree.FromBytes(reader);
            
                return result;
            }).Where(result =>
            {
                var treeValue = result.Tree.GetAsString(request.TreeKey);
                if (treeValue == null) return false;
                if (request.TreeValue == null) return true;
                
                return treeValue == request.TreeValue;
            });
        }
        
        foreach (var result in results)
        {
            var treeResult = "";
            var treeValue = result.Tree.GetAsString(request.TreeKey);
            if (treeValue != null) treeResult = $": {request.TreeKey} = {treeValue}";
            Console.WriteLine($"{filePath}: block = {result.AssetLocation}, index = {result.Index}{treeResult}");
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

        foreach (var modPath in Directory.EnumerateDirectories(assetPath))
        {
           ProcessModPath(modPath, request);
        }
    }

    public static void Main(string[] args)
    {
        if (args.Length <= 0) throw new Exception("Usage: schematiccli [options]");

        var request = new Request(
            Domain: args.Length <= 2 ? "game" : args[1],
            PartialPath: args.Length <= 2 ? "tapestry" : args[2],
            TreeKey: "type",
            TreeValue: null);
        
        Run(args[0], request);
    }
}