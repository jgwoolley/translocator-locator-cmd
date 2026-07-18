#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Nf3t.VintageStory.SchematicLocator;

public class SchematicLocatorModSystem : ModSystem
{
    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);

        api.ChatCommands.Create("findschematics")
            .WithDescription("Finds structures by block id")
            .RequiresPrivilege(Privilege.controlserver)
            .WithArgs(
                api.ChatCommands.Parsers.Word("searchBlock"),
                api.ChatCommands.Parsers.OptionalWord("treeValue"),
                api.ChatCommands.Parsers.OptionalWord("domain"))
            .HandleWith(args => FindSchematics(api, args));
    }

    private static TextCommandResult FindSchematics(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        var searchBlockPrefix = (string) args[0];
        if (searchBlockPrefix == null)
        {
            return TextCommandResult.Error("[Translocator Locator] Invalid searchBlockPrefix specified");
        }    
        var treeKey = (string) args[1];
        var treeValue = (string?) args[2];
        var domain = (string?) args[3];

        
        var assets = api.Assets.GetMany("worldgen/schematics/", domain);
        var count = 0;
        var error = string.Empty;
        foreach (var asset in assets)
        {
            try
            {
                var schematic = asset.ToObject<BlockSchematic>();

                var paletteIds = new HashSet<int>();
                foreach (var (paletteId, blockCode) in schematic.BlockCodes)
                {
                    if (blockCode.PathStartsWith(searchBlockPrefix))
                    {
                        paletteIds.Add(paletteId);
                    }
                }

                if(paletteIds.Count == 0) continue;
                
                if (!string.IsNullOrEmpty(treeKey))
                {
                    var found = false;
                    for (var positionId = 0; positionId < schematic.BlockIds.Count; positionId++)
                    {
                        var paletteId = schematic.BlockIds[positionId];
                        if (!paletteIds.Contains(paletteId)) continue;
                        var packedCoordinate = schematic.Indices[positionId];
                        if (schematic.BlockEntities.TryGetValue(packedCoordinate, out var rawBlockEntity))
                        {
                            if (rawBlockEntity == null) continue;
                            var beBytes = Ascii85.Decode(rawBlockEntity);
                            if (beBytes == null) continue;
                            using var ms = new MemoryStream(beBytes);
                            var reader = new BinaryReader(ms);
                            var tree = new TreeAttribute();
                            tree.FromBytes(reader);

                            var actualTreeValue = tree.GetAsString(treeKey);
                            if (actualTreeValue == null) continue;
                            if (!string.IsNullOrEmpty(treeValue))
                            {
                                if (!actualTreeValue.StartsWith(treeValue)) continue;

                            }

                            found = true;
                            break;
                        }
                    }
                    
                    if (!found) continue;
                }
                
                var message = $"Found schematic {asset} with block {searchBlockPrefix}.";
                api.SendMessage(args.Caller.Player, GlobalConstants.GeneralChatGroup, message, EnumChatType.Notification);
                count++;
            }
            catch
            {
                api.Logger.Error($"Failed to load schematic {asset}: {error}");
            }
        }
    
        return TextCommandResult.Success("[Translocator Locator] Found " + count + " structure(s)");
    }
}