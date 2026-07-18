#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Nf3t.VintageStory.Sponge;

public class CenterMapModSystem : ModSystem
{
    private BlockPos? PanToCoordinates(ICoreClientAPI capi, Vec3d coordinates)
    {
        var mapDialog = capi.Gui.LoadedGuis.Find(g => g is GuiDialogWorldMap) as GuiDialogWorldMap;

        if (mapDialog == null) return null;
        
        if (mapDialog.DialogType != EnumDialogType.Dialog)
        {
            mapDialog.Open(EnumDialogType.Dialog);
        }

        if (mapDialog.SingleComposer?.GetElement("mapElem") is not GuiElementMap mapElem) return null;
        var targetPos = new BlockPos((int)coordinates.X, 0, (int)coordinates.Z);
        mapElem.CenterMapTo(targetPos);
        return targetPos;

    }
    
    private TextCommandResult HandlePanMap(ICoreClientAPI capi, TextCommandCallingArgs args)
    {
        var coordinates = (Vec3d)args[0];
                
        var targetPos = PanToCoordinates(capi, coordinates);

        if(targetPos == null) return TextCommandResult.Error("Could not open World Map");

        SendPanmapLink(capi, coordinates);
        
        return TextCommandResult.Success("Centered: " + targetPos);
    }
    
    public override void StartClientSide(ICoreClientAPI api)
    {
        
        api.RegisterLinkProtocol("panmap", text => HandlePanMapProtocol(api, text));
        
        api.ChatCommands.Create("panmap").WithDescription("Pans the world map to specific coordinates")
            .WithArgs(api.ChatCommands.Parsers.OptionalWorldPosition("coordinates")).HandleWith(args => HandlePanMap(api, args));
        
    }

    private void HandlePanMapProtocol(ICoreClientAPI capi, LinkTextComponent component)
    {
        var refId = component.Href;
        var parts = refId.Replace("panmap://", "").Split(",");

        // Parse the coordinates from the "refId" (e.g., "panmap://100,50,-300")
        if (parts.Length == 3 &&
            double.TryParse(parts[0], out var x) &&
            double.TryParse(parts[1], out var y) &&
            double.TryParse(parts[2], out var z))
        {
            var coordinates = new Vec3d(x, y, z);

            // Call the map panning functionality with the parsed coordinates
            PanToCoordinates(capi, coordinates);
        }
        else
        {
            capi.ShowChatMessage("[ERROR] Invalid panmap protocol format. Expected panmap://x,y,z");
        }
    }
    
    private void SendPanmapLink(ICoreClientAPI capi, Vec3d coordinates)
    {
        var clickableText = $"<a href=\"panmap://{coordinates.X},{coordinates.Y},{coordinates.Z}\">[Click to Pan to Location]</a>";
        
        // Create the LinkTextComponent and define onClick behavior
        var linkComponent = new LinkTextComponent(capi, clickableText,
            CairoFont.WhiteDetailText(),
            component => HandlePanMapProtocol(capi, component));

        // Send the clickable link to a specific chat group (e.g., general chat group)
        capi.SendChatMessage(clickableText, GlobalConstants.GeneralChatGroup);
        capi.ShowChatMessage(clickableText);
    }
}