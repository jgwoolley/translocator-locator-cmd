// See https://aka.ms/new-console-template for more information

using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using System.Linq;

void Process(string fileName, BlockSchematic data)
{
    var results = data.Indices.Zip(data.BlockIds, (uint index, int blockId) =>
    {
        data.BlockCodes.TryGetValue(blockId, out var assetLocation);
        var tree = new TreeAttribute();
        var result = new Result(index, blockId, assetLocation, tree);

        if (data.BlockEntities.TryGetValue(index, out var rawBlockEntity))
        {
            var beBytes = Ascii85.Decode(rawBlockEntity);
            if (beBytes != null)
            {
                using (MemoryStream ms = new MemoryStream(beBytes))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    tree.FromBytes(reader);
                }
            }
        }
        
        return result;
    });

    var types = new HashSet<string>();
    foreach (var result in results)
    {
        if (result.assetLocation == null || !result.assetLocation.BeginsWith("game", "tapestry"))
        {
            continue;
        }

        var type = result.tree.GetAsString("type");
        if (type == null)
        {
            continue;
        }
        types.Add(type);
    }

    if (types.Count == 0)
    {
        return;
    }

    if (!types.Contains("schematic-b12"))
    {
        return;
    }
    
    Console.WriteLine($"{fileName}: {string.Join(", ", types)}");
}

void Main(string basePath)
{
    // Define the root path (Mod/assets/game/worldgen/schematics)
    string rootPath = Path.Combine(basePath, "assets", "game", "worldgen", "schematics");
    Console.WriteLine(rootPath);
    if (!Directory.Exists(rootPath)) return;

    // SearchOption.AllDirectories mimics Python's .rglob('*.json')
    var files = Directory.GetFiles(rootPath, "*.json", SearchOption.AllDirectories);
    
    foreach (var filePath in files)
    {
        try 
        {
            // Read the file content
            var jsonContent = File.ReadAllText(filePath);

            // Deserialize into your BlockSchematic class
            var data = JsonConvert.DeserializeObject<BlockSchematic>(jsonContent);
            if (data == null)
            {
                continue;
            }

            var fileName = Path.GetFileName(filePath);
            Process(fileName, data);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading {filePath}: {ex.Message}");
        }
    }
}

if (args.Length <= 0)
{
    throw new Exception("Usage: schematiccli [options]");
}

Main(args[0]);

record struct Result(uint index, int blockId, AssetLocation? assetLocation, ITreeAttribute tree);