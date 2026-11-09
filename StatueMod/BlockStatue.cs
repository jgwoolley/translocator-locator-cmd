using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Statue;

public class BlockStatue : Block
{
    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel,
        ItemStack byItemStack)
    {
        if (!base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack)) return false;

        // Face the player, in steps of 22.5 degrees.
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityStatue statue)
        {
            var dx = byPlayer.Entity.Pos.X - (blockSel.Position.X + blockSel.HitPosition.X);
            var dz = byPlayer.Entity.Pos.Z - (blockSel.Position.Z + blockSel.HitPosition.Z);
            var step = GameMath.PIHALF / 4;
            statue.MeshAngleY = (float)System.Math.Round(StatueFacing.Toward(dx, dz) / step) * step;
        }

        return true;
    }

    /// <summary>The lang key of the statue's name; the material is filled in by <see cref="StatueMaterial.NameOf"/>.</summary>
    protected virtual string NameKey => "nf3tstatue:name-" + Variant["size"];

    // Lang entries cannot see the rock variant, so the name is built here from the game's own rock names.
    private string DisplayName() => StatueMaterial.NameOf(this, NameKey);

    public override string GetHeldItemName(ItemStack itemStack) => DisplayName();

    public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos) => DisplayName();

    private const string MeshRefsKey = "nf3tstatueMeshRefs";
    private const int MaxMeshRefs = 300;

    /// <summary>
    /// A statue item shows its figure in inventories, on the ground and in the edit dialog preview. The figure is
    /// built from the settings kept on the item, and cached per settings.
    /// </summary>
    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target,
        ref ItemRenderInfo renderinfo)
    {
        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        if (itemstack.Attributes[BlockEntityStatue.AttributesKey] is not ITreeAttribute tree) return;

        var emote = tree.GetString("emote", "");
        var type = tree.GetString("statueType", StatueTypes.Player);
        if (emote == "" && type == StatueTypes.Player) return;

        var frame = tree.GetInt("frame", -1);
        var angle = tree.GetFloat("previewAngle");
        var appearance = StatueAppearance.Read(tree, capi.World);
        var key = string.Join('|', Code, type, emote, frame, angle, appearance.Mount, appearance.ElkPose, string.Join(';', appearance.Tack), appearance.Antlers, appearance.NoPedestal, string.Join(';', appearance.SkinShapes),
            string.Join(';', appearance.Clothing.Select(s => s.Collectible.Code)),
            string.Join(';', appearance.Armor.Select(s => s.Collectible.Code)),
            appearance.RightHand?.Collectible.Code, appearance.LeftHand?.Collectible.Code);

        var meshRefs = ObjectCacheUtil.GetOrCreate(capi, MeshRefsKey, () => new Dictionary<string, MultiTextureMeshRef?>());
        if (!meshRefs.TryGetValue(key, out var meshRef))
        {
            if (meshRefs.Count >= MaxMeshRefs) DisposeMeshRefs(capi);

            var bust = Variant["size"] == "bust";
            var mounted = appearance.Mount != StatueAppearance.NoMount;
            // Inventory icons (and the dialog preview) are one block wide. A large statue standing on its own wide
            // plinth is built as it stands in the world and then shrunk as a whole, so figure and plinth keep their
            // proportions; drawing the figure at the small size on the full-size 3x3 plinth made the plinth dwarf it.
            var shrinkLarge = Variant["size"] == "large" && !mounted && !appearance.NoPedestal;
            var mesh = StatueMesh.Build(capi, capi.Tesselator, this, emote, frame, appearance, bust,
                bust ? BlockEntityStatue.BustScale
                    : shrinkLarge ? BlockEntityStatue.LargeScale
                    : mounted ? BlockEntityStatue.RideSmallScale : BlockEntityStatue.SmallScale, angle, icon: !shrinkLarge);
            if (mesh != null && !mounted && !appearance.NoPedestal)
            {
                capi.Tesselator.TesselateBlock(this, out var pedestal);
                mesh.AddMeshData(pedestal);
            }

            if (mesh != null && shrinkLarge)
            {
                const float k = BlockEntityStatue.SmallScale / BlockEntityStatue.LargeScale;
                mesh.Scale(new Vec3f(0.5f, 0, 0.5f), k, k, k);
            }

            meshRef = meshRefs[key] = mesh == null ? null : capi.Render.UploadMultiTextureMesh(mesh);
        }

        if (meshRef != null) renderinfo.ModelRef = meshRef;
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        base.OnUnloaded(api);
        if (api is ICoreClientAPI capi) DisposeMeshRefs(capi);
    }

    private static void DisposeMeshRefs(ICoreClientAPI capi)
    {
        var meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef?>>(capi, MeshRefsKey);
        if (meshRefs == null) return;

        foreach (var meshRef in meshRefs.Values) meshRef?.Dispose();
        meshRefs.Clear();
    }

    /// <summary>
    /// A finished stone statue is also the bed of a mold: pack fire clay around it, one piece at a time, and when it is
    /// full take the mold from it. Anything else is left to the game.
    /// </summary>
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityStatue { IsConfigured: true } statue
            && statue.OnMoldInteract(byPlayer))
            return true;

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection,
        IPlayer forPlayer)
    {
        if (Variant["rock"] == null || world.BlockAccessor.GetBlockEntity(selection.Position) is not BlockEntityStatue { IsConfigured: true } statue)
            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);

        var full = statue.ClayLevel >= StatueMolding.ClayNeeded(Variant["size"]);
        return
        [
            new WorldInteraction
            {
                ActionLangCode = full ? "nf3tstatue:help-take-mold" : "nf3tstatue:help-clay",
                MouseButton = EnumMouseButton.Right,
                Itemstacks = full ? null : [new ItemStack(world.GetItem(new AssetLocation("game:clay-fire")), 1)],
            },
        ];
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer,
        float dropQuantityMultiplier = 1)
    {
        var stack = new ItemStack(this);
        (world.BlockAccessor.GetBlockEntity(pos) as BlockEntityStatue)?.WriteToStack(stack);
        return [stack];
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        var stack = new ItemStack(this);
        (world.BlockAccessor.GetBlockEntity(pos) as BlockEntityStatue)?.WriteToStack(stack);
        return stack;
    }
}
