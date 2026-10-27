using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
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

    // Lang entries cannot see the rock variant, so the name is built here from the game's own rock names.
    private string DisplayName() =>
        Lang.Get("nf3tstatue:name-" + Variant["size"], Variant["metal"] is { } metal
            ? Lang.Get("nf3tstatue:metal-" + metal)
            : Lang.Get("rock-" + Variant["rock"]));

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
        if (emote == "") return;

        var frame = tree.GetInt("frame", -1);
        var angle = tree.GetFloat("previewAngle");
        var appearance = StatueAppearance.Read(tree, capi.World);
        var key = string.Join('|', Code, emote, frame, angle, appearance.Mount, appearance.ElkPose, string.Join(';', appearance.SkinShapes),
            string.Join(';', appearance.Clothing.Select(s => s.Collectible.Code)),
            string.Join(';', appearance.Armor.Select(s => s.Collectible.Code)),
            appearance.RightHand?.Collectible.Code, appearance.LeftHand?.Collectible.Code);

        var meshRefs = ObjectCacheUtil.GetOrCreate(capi, MeshRefsKey, () => new Dictionary<string, MultiTextureMeshRef?>());
        if (!meshRefs.TryGetValue(key, out var meshRef))
        {
            if (meshRefs.Count >= MaxMeshRefs) DisposeMeshRefs(capi);

            var bust = Variant["size"] == "bust";
            // Inventory icons are one block wide, so a large statue is drawn at the small size.
            var mesh = StatueMesh.Build(capi, capi.Tesselator, this, emote, frame, appearance, bust,
                Variant["size"] == "bust" ? BlockEntityStatue.BustScale
                    : appearance.Mount != StatueAppearance.NoMount ? BlockEntityStatue.RideSmallScale : BlockEntityStatue.SmallScale, angle);
            if (mesh != null && appearance.Mount == StatueAppearance.NoMount)
            {
                capi.Tesselator.TesselateBlock(this, out var pedestal);
                mesh.AddMeshData(pedestal);
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

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.Side != EnumAppSide.Client) return true;
        if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            return base.OnBlockInteractStart(world, byPlayer, blockSel);

        (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityStatue)?.OpenDialog();
        return true;
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

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection,
        IPlayer forPlayer)
    {
        return
        [
            new WorldInteraction
            {
                ActionLangCode = "nf3tstatue:help-edit",
                MouseButton = EnumMouseButton.Right,
            },
        ];
    }
}
