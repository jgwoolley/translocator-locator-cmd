using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Nf3t.VintageStory.Statue;

/// <summary>
/// Finds the shape files (hair, face, beard...) a player's seraph is wearing.
/// </summary>
public static class StatueSkin
{
    public static string[] ShapesOf(IPlayer player)
    {
        var skinnable = player.Entity?.GetBehavior<EntityBehaviorExtraSkinnable>();
        if (skinnable == null) return [];

        var applied = new Dictionary<string, string>();
        foreach (var part in skinnable.AppliedSkinParts) applied[part.PartCode] = part.Code;

        var shapes = new List<string>();
        foreach (var part in skinnable.AppliedSkinParts)
        {
            var available = skinnable.AvailableSkinPartsByCode[part.PartCode];
            if (available.Type != EnumSkinnableType.Shape) continue;

            var composite = part.Shape ?? available.ShapeTemplate;
            if (composite?.Base == null) continue;

            var path = composite.Base.Path;
            if (part.Shape == null) path = path.Replace("{code}", part.Code);
            foreach (var (partCode, variantCode) in applied) path = path.Replace("{" + partCode + "}", variantCode);

            shapes.Add(composite.Base.Domain + ":" + path);
        }

        return shapes.ToArray();
    }

    public static IPlayer? FindByName(ICoreServerAPI sapi, string name)
    {
        foreach (var player in sapi.World.AllOnlinePlayers)
            if (string.Equals(player.PlayerName, name, System.StringComparison.OrdinalIgnoreCase))
                return player;
        return null;
    }
}
