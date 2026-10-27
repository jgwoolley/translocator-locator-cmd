using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Nf3t.VintageStory.Statue;

/// <summary>
/// Builds the posed, stone-textured seraph mesh of a statue.
/// </summary>
public static class StatueMesh
{
    private const string BodyShape = "game:shapes/entity/humanoid/seraph-faceless.json";
    private static readonly HashSet<string> BustBones = ["Neck", "Head", "UpperTorso", "LowerTorso"];
    private const float PedestalHeight = 2 / 16f;

    private static readonly Dictionary<EnumAppSide, SeraphData> Loaded = new();

    /// <summary>The seraph shape and its emotes, loaded once per side (client and server load them differently).</summary>
    private record SeraphData(Shape Shape, Dictionary<string, Animation> Animations);

    public static void ClearCache()
    {
        lock (Loaded) Loaded.Clear();
    }

    private static SeraphData? Seraph(ICoreAPI api)
    {
        lock (Loaded)
        {
            if (Loaded.TryGetValue(api.Side, out var data)) return data;

            var shape = Shape.TryGet(api, new AssetLocation(BodyShape));
            if (shape == null)
            {
                api.Logger.Warning("[nf3tstatue] Could not load {0}", BodyShape);
                return null;
            }

            var animations = (shape.Animations ?? []).GroupBy(a => a.Code).ToDictionary(g => g.Key, g => g.First());
            return Loaded[api.Side] = new SeraphData(shape, animations);
        }
    }

    public static Animation? AnimationOf(ICoreAPI api, string emote) =>
        Seraph(api)?.Animations.GetValueOrDefault(emote);

    public static int FrameCount(ICoreAPI api, string emote) => AnimationOf(api, emote)?.QuantityFrames ?? 1;

    /// <summary>The frame where the emote peaks (for a bust, judged by the head and torso only).</summary>
    public static int PeakFrame(ICoreAPI api, string emote, bool bust)
    {
        var animation = AnimationOf(api, emote);
        return animation == null ? 0 : StatuePose.PeakFrame(animation, bust ? BustBones : null);
    }

    /// <summary>A real frame number: automatic (-1) becomes the peak frame, and others are kept within the emote.</summary>
    public static int ResolveFrame(ICoreAPI api, string emote, int frame, bool bust) =>
        frame < 0 ? PeakFrame(api, emote, bust) : Math.Clamp(frame, 0, Math.Max(0, FrameCount(api, emote) - 1));

    public static MeshData? Build(ICoreClientAPI capi, ITesselatorAPI tesselator, Block block, string emote, int frame,
        StatueAppearance appearance, bool bust, float scale, float angleY)
    {
        var seraph = Seraph(capi);
        if (seraph == null) return null;

        // The animations are not needed for a static mesh, and cloning all of them is slow.
        Shape shape;
        lock (Loaded)
        {
            shape = new Shape
            {
                Elements = seraph.Shape.CloneElements(),
                Animations = [],
                TextureWidth = seraph.Shape.TextureWidth,
                TextureHeight = seraph.Shape.TextureHeight,
                Textures = new Dictionary<string, AssetLocation>(),
                TextureSizes = new Dictionary<string, int[]>(),
            };
        }

        foreach (var skinShape in appearance.SkinShapes) AddSkinPart(capi, shape, skinShape);
        foreach (var gear in appearance.Clothing) AddGear(capi, shape, gear);
        foreach (var gear in appearance.Armor) AddGear(capi, shape, gear);

        // The animation is chosen by emote code, which is the shape animation's name for every vanilla emote.
        if (seraph.Animations.TryGetValue(emote, out var animation))
        {
            var keyFrame = frame < 0
                ? StatuePose.PickPeakKeyFrame(animation, bust ? BustBones : null)
                : StatuePose.PoseAt(animation, frame);
            if (keyFrame != null) StatuePose.Apply(shape, keyFrame);
        }

        if (bust) StatuePose.HideExcept(shape, StatuePose.SubtreeNames(shape, "Neck"));
        StatuePose.UseSingleTile(shape);

        tesselator.TesselateShape(new TesselationMetaData
        {
            TypeForLogging = "statue",
            TexSource = new StoneTextureSource(capi, block),
        }, shape, out var mesh);

        if (!bust)
        {
            AddHeldItem(capi, tesselator, block, shape, mesh, appearance.RightHand, "ItemAnchor", "RightHand");
            AddHeldItem(capi, tesselator, block, shape, mesh, appearance.LeftHand, "ItemAnchorL", "LeftHand");
        }

        var center = new Vec3f(0.5f, 0, 0.5f);
        if (bust) CenterOnPedestal(mesh);
        mesh.Scale(center, scale, scale, scale);
        mesh.Translate(0, PedestalHeight, 0);
        mesh.Rotate(center, 0, angleY, 0);
        return mesh;
    }

    /// <summary>
    /// Puts an item in a hand, in stone. Its own shape is meshed, then moved to the hand the way the game holds it in
    /// third person: the (posed) hand anchor, then the item's hand transform.
    /// </summary>
    private static void AddHeldItem(ICoreClientAPI capi, ITesselatorAPI tesselator, Block block, Shape body,
        MeshData mesh, ItemStack? stack, string anchor, string point)
    {
        if (stack == null) return;

        var collectible = stack.Collectible;
        var composite = collectible is Item item ? item.Shape : (collectible as Block)?.Shape;
        if (composite?.Base == null) return;

        var location = composite.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");
        var shape = Shape.TryGet(capi, location);
        var matrix = StatuePose.AnchorMatrix(body, anchor, point);
        if (shape == null || matrix == null)
        {
            capi.Logger.Warning("[nf3tstatue] Cannot hold {0}: shape {1} or hand {2} missing", collectible.Code, location, anchor);
            return;
        }

        StatuePose.UseSingleTile(shape);
        tesselator.TesselateShape(new TesselationMetaData
        {
            TypeForLogging = "statue-held",
            TexSource = new StoneTextureSource(capi, block),
            Rotation = new Vec3f(composite.rotateX, composite.rotateY, composite.rotateZ),
        }, shape, out var itemMesh);

        if (collectible.TpHandTransform != null) Mat4f.Mul(matrix, matrix, collectible.TpHandTransform.AsMatrix);
        mesh.AddMeshData(itemMesh.MatrixTransform(matrix));
    }

    /// <summary>
    /// Adds a worn item the way the game dresses a mannequin: hide the body parts it replaces, then step-parent its
    /// shape onto the body. Items that only recolor the skin have no shape and add nothing to a stone statue.
    /// </summary>
    private static void AddGear(ICoreClientAPI capi, Shape body, ItemStack stack)
    {
        var attachable = IAttachableToEntity.FromCollectible(stack.Collectible);
        if (attachable == null || !attachable.IsAttachable(null!, stack)) return;

        var composite = attachable.GetAttachedShape(stack, "default");
        if (composite?.Base == null) return;

        var location = composite.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");
        var part = Shape.TryGet(capi, location);
        if (part == null)
        {
            capi.Logger.Warning("[nf3tstatue] Gear shape {0} for {1} not found", location, stack.Collectible.Code);
            return;
        }

        var keep = attachable.GetKeepElements(stack) ?? [];
        var hide = (attachable.GetDisableElements(stack) ?? []).Except(keep).ToArray();
        body.RemoveElements(hide);

        part.SubclassForStepParenting((attachable.GetTexturePrefixCode(stack) ?? stack.Collectible.Code.Path) + "-");
        body.StepParentShape(part, location.ToShortString(), BodyShape, capi.Logger, (_, _) => { });
    }

    /// <summary>The head sits high on the body, so move whatever is left to stand on the pedestal.</summary>
    private static void CenterOnPedestal(MeshData mesh)
    {
        if (mesh.VerticesCount == 0) return;

        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, minZ = float.MaxValue, maxZ = float.MinValue;
        for (var i = 0; i < mesh.VerticesCount; i++)
        {
            var x = mesh.xyz[i * 3];
            var z = mesh.xyz[i * 3 + 2];
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            minY = Math.Min(minY, mesh.xyz[i * 3 + 1]);
            minZ = Math.Min(minZ, z);
            maxZ = Math.Max(maxZ, z);
        }

        mesh.Translate(0.5f - (minX + maxX) / 2, -minY, 0.5f - (minZ + maxZ) / 2);
    }

    private static void AddSkinPart(ICoreClientAPI capi, Shape body, string skinShape)
    {
        var location = new AssetLocation(skinShape).CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");
        var part = Shape.TryGet(capi, location);
        if (part == null)
        {
            capi.Logger.Warning("[nf3tstatue] Skin shape {0} not found", location);
            return;
        }

        part.SubclassForStepParenting("skinpart-");
        body.StepParentShape(part, location.ToShortString(), BodyShape, capi.Logger, (_, _) => { });
    }

    /// <summary>
    /// Every texture code of the shape resolves to the statue's stone.
    /// </summary>
    private class StoneTextureSource(ICoreClientAPI capi, Block block) : ITexPositionSource
    {
        private readonly ITexPositionSource blockSource = capi.Tesselator.GetTextureSource(block);

        public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

        public TextureAtlasPosition this[string textureCode] => blockSource["stone"]!;
    }
}
