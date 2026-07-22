#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Nf3t.VintageStory.SchematicLocator;

public class SchematicLocatorModSystem : ModSystem
{
    private const string ResultChannel = "nf3tschematiclocatorresult";
    private const string RequestChannel = "nf3tschematiclocatorrequest";

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        var requestChannel = api.Network.RegisterChannel(RequestChannel).RegisterMessageType<SchematicSearchRequest>();
        
        var dialog = new StringTableDialog(api, requestChannel);
        
        api.Network.RegisterChannel(ResultChannel).RegisterMessageType<SchematicSearchResults>().SetMessageHandler<SchematicSearchResults>(packet =>
        {
            api.Logger.Debug($"Recieving {packet.Results.Count} result(s)");
            dialog.Update(packet);
        });
        
        api.ChatCommands.Create("findschematics").HandleWith(c =>
        {
            // 3. Toggle it: Close if open, Open if closed
            if (dialog.IsOpened())
            {
                dialog.TryClose();
            }
            dialog.TryOpen();
            
            return TextCommandResult.Success();
        });
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        var resultChannel = api.Network.RegisterChannel(ResultChannel).RegisterMessageType<SchematicSearchResults>();
        api.Network.RegisterChannel(RequestChannel).RegisterMessageType<SchematicSearchRequest>().SetMessageHandler<SchematicSearchRequest>((
            fromPlayer, packet) =>
        {
            var results = FindSchematicsCommand(api, packet);
            var message = new SchematicSearchResults
            {
                Results = results
            };
            api.Logger.Debug($"Sending results [{results.Count}] packet: {packet} to {fromPlayer}");
            resultChannel.SendPacket(message, [fromPlayer]);
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
        var assets = api.Assets.GetMany("worldgen/schematics/", string.IsNullOrEmpty(request.Domain) ? null: request.Domain);
        var results = new List<SchematicSearchResult>();
        var error = string.Empty;
        foreach (var asset in assets)
        {
            try
            {
                var schematic = asset.ToObject<BlockSchematic>();

                var paletteIds = new HashSet<int>();
                foreach (var (paletteId, blockCode) in schematic.BlockCodes)
                {
                    if (blockCode.PathStartsWith(request.SearchBlockPrefix))
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
                            AssetLocation = asset.Location,
                            MatchedBlock = blockCode.Path,
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
                            AssetLocation = asset.Location,
                            MatchedBlock = blockCode.Path,
                            Count = 1,
                        });
                    }
                }
            }
            catch
            {
                api.Logger.Error($"Failed to load schematic {asset}: {error}");
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