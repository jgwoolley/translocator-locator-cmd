#nullable enable
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Nf3t.VintageStory.SchematicLocator;

public class Utils
{
    public static string NormalizeAssetLocationPath(string assetLocationPath)
    {
        if (assetLocationPath.EndsWith("-south") || assetLocationPath.EndsWith("-west") ||
            assetLocationPath.EndsWith("-east") || assetLocationPath.EndsWith("-north"))
        {
            var lastHyphen = assetLocationPath.LastIndexOf('-');
            assetLocationPath = assetLocationPath[..lastHyphen];
        }

        return assetLocationPath;
    }
    
    public static string NorthifyAssetLocationPath(string assetLocationPath)
    {
        if (assetLocationPath.EndsWith("-south") || assetLocationPath.EndsWith("-west") ||
            assetLocationPath.EndsWith("-east"))
        {
            var lastHyphen = assetLocationPath.LastIndexOf('-');
            assetLocationPath = assetLocationPath[..lastHyphen] + "-north";
        }

        return assetLocationPath;
    }

    public static bool MatchAssetLocation(ICoreAPI api, AssetLocation? assetLocation, string searchBlockPrefix)
    {
        if (assetLocation == null) return false;
        if (assetLocation.PathStartsWith(searchBlockPrefix)) return true;
        
        var block = api.World.GetBlock(assetLocation);
        var stack = new ItemStack(block);
        var blockName = stack.GetName();

        return blockName.StartsWith(searchBlockPrefix);
    }
}