#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Nf3t.VintageStory.SchematicLocator;

public class SchematicLocatorModSystem : ModSystem
{
    public const string Title = "Schematic Locator";
    public const string NetworkChannel = "nf3tschematiclocator";
    public const string HotkeyCode = "nf3tschematiclocator";
    
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        var networkChannel = api.Network.RegisterChannel(NetworkChannel);
        networkChannel.RegisterMessageType<SchematicSearchRequest>();
        networkChannel.RegisterMessageType<SchematicSearchResults>();
        networkChannel.RegisterMessageType<SchematicContentRequest>();
        networkChannel.RegisterMessageType<SchematicContentResult>();
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        
        var dialog = new SchematicSearchDialog(api);
        
        var networkChannel = api.Network.GetChannel(NetworkChannel);
        networkChannel.SetMessageHandler<SchematicSearchResults>(packet =>
        {
            api.Logger.Debug($"SchematicSearchResults (Client): Receiving {packet.Results.Count} result(s)");
            dialog.Update(packet);
        });
        networkChannel.SetMessageHandler<SchematicContentResult>(packet =>
        {
            api.Logger.Debug("SchematicContentResult (Client): Received Schematic Json: " + packet.AssetLocation);
            
            var worldEditDir = Path.Combine(GamePaths.DataPath, "WorldEdit");
            
            // Make sure the directory exists
            Directory.CreateDirectory(worldEditDir);
            
            var filename = packet.AssetLocation.Split('/').Last();
            var filenameWithoutExt = Path.GetFileNameWithoutExtension(filename);
            var filePath = Path.Combine(worldEditDir, filename);
            File.WriteAllText(filePath, packet.Content);
            
            var schematic = JsonUtil.FromString<BlockSchematic>(packet.Content);
            var message = "Received Schematic Json: " + packet.AssetLocation.ToString();
            if (schematic != null)
            {
                message += ": " + schematic.SizeX + "x" + schematic.SizeY + "x" + schematic.SizeZ;
            }

            var command = $"/we imp {filenameWithoutExt}";
            message += $"\nTo spawn it\n/we start\n/we end\n{command}.\nThe last command has been put in your clipboard: {command}";
            
            api.Forms.SetClipboardText(command);
            api.ShowChatMessage(message);
        });
        
        api.Input.RegisterHotKey(hotkeyCode: HotkeyCode, name: Title, key: GlKeys.Y, type: HotkeyType.GUIOrOtherControls, shiftPressed: false, ctrlPressed: true, altPressed: false);
        api.Input.SetHotKeyHandler(HotkeyCode, _ =>
        {
            if (dialog.IsOpened())
            {
                dialog.TryClose();
            }
            dialog.TryOpen();
            return true;
        });
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        StructureCommands.Register(api);
        var networkChannel = api.Network.GetChannel(NetworkChannel);
        networkChannel.SetMessageHandler<SchematicSearchRequest>((
            fromPlayer, packet) =>
        {
            var results = FindSchematicsCommand(api, packet);
            var message = new SchematicSearchResults
            {
                Results = results
            };
            api.Logger.Debug($"Sending results [{results.Count}] packet: {packet} to {fromPlayer}");
            networkChannel.SendPacket(message, [fromPlayer]);
        });
        networkChannel.SetMessageHandler<SchematicContentRequest>((fromPlayer, packet) =>
        {
            api.Logger.Debug($"SchematicContentRequest (Server): Looking up {packet.AssetLocation}");
            var schematic = api.Assets.Get<BlockSchematic>(packet.AssetLocation);
            var response = new SchematicContentResult()
            {
                AssetLocation = packet.AssetLocation,
                Content = schematic.ToJson(),
            };
            networkChannel.SendPacket(response, [fromPlayer]);
            api.Logger.Debug($"SchematicContentRequest (Server): Sent {packet.AssetLocation}");
        });
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="api"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    private static List<SchematicSearchResult> FindSchematicsCommand(ICoreServerAPI api, SchematicSearchRequest request)
    {
        var assets = api.Assets.GetMany<BlockSchematic>(api.Logger, "worldgen/schematics/", string.IsNullOrEmpty(request.Domain) ? null: request.Domain);
        var results = new List<SchematicSearchResult>();
        foreach (var (asset, schematic) in assets)
        {
            try
            {
                var paletteIds = new HashSet<int>();
                foreach (var (paletteId, blockCode) in schematic.BlockCodes)
                {
                    if (Utils.MatchAssetLocation(api, blockCode, request.SearchBlockPrefix))
                    {
                        paletteIds.Add(paletteId);
                    }
                }

                if(paletteIds.Count == 0) continue;
                
                var tree = new TreeAttribute();
                using var ms = new MemoryStream();
                var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);
                for (var positionId = 0; positionId < schematic.BlockIds.Count; positionId++)
                {
                    var paletteId = schematic.BlockIds[positionId];
                    var blockCode = schematic.BlockCodes[paletteId];
                    if (!paletteIds.Contains(paletteId)) continue;
                    if (string.IsNullOrEmpty(request.TreeKey))
                    {
                        results.Add(new SchematicSearchResult()
                        {
                            AssetLocation = asset,
                            MatchedBlock = blockCode.ToShortString(),   // with its domain for mod blocks (the client looks it up)
                            Count = 1,
                        });
                        continue;
                    }
                    
                    var packedCoordinate = schematic.Indices[positionId];
                    if (schematic.BlockEntities.TryGetValue(packedCoordinate, out var rawBlockEntity))
                    {
                        if (rawBlockEntity == null) continue;
                        var beBytes = Ascii85.Decode(rawBlockEntity);
                        if (beBytes == null) continue;
                        
                        ms.SetLength(0);
                        ms.Write(beBytes, 0, beBytes.Length);
                        ms.Position = 0;

                        tree.FromBytes(reader);

                        var actualTreeValue = tree.GetAsString(request.TreeKey);
                        if (actualTreeValue == null) continue;
                        if (!string.IsNullOrEmpty(request.TreeValue))
                        {
                            if (!actualTreeValue.StartsWith(request.TreeValue)) continue;
                        }

                        results.Add(new SchematicSearchResult()
                        {
                            AssetLocation = asset,
                            MatchedBlock = blockCode.ToShortString(),   // with its domain for mod blocks (the client looks it up)
                            Count = 1,
                        });
                    }
                }
            }
            catch (System.Exception e)
            {
                api.Logger.Error($"Failed to load schematic {asset}: {e}");
            }
        }
        
        return results.GroupBy(result => new { result.AssetLocation, result.MatchedBlock }).Select(group => new SchematicSearchResult
        {
            AssetLocation = group.Key.AssetLocation,
            MatchedBlock = group.Key.MatchedBlock,
            Count = group.Sum(result => result.Count)
        }).ToList();
    }
}