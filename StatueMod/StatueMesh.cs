using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
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

    private static readonly HashSet<string> RiderLeanBones = ["UpperTorso", "Neck", "Head"];
    private static readonly HashSet<string> RiderEmoteBones = ["UpperArmR", "LowerArmR", "UpperArmL", "LowerArmL", "Neck", "Head"];
    private const string ElkShape = "game:shapes/entity/animal/mammal/hooved/deer/elk/female.json";
    private const string ElkBullPartsPath = "game:shapes/entity/animal/mammal/hooved/deer/elk/";
        /// <summary>The rider sits this far below the seat attachment point, as in the elk's seat config.</summary>
    private const float RiderOffsetY = -0.55f;

    private static readonly Dictionary<(EnumAppSide, string), ShapeData> Loaded = new();

    /// <summary>A loaded shape and its animations, kept once per side (client and server load them differently).</summary>
    private record ShapeData(Shape Shape, Dictionary<string, Animation> Animations);

    public static void ClearCache()
    {
        lock (Loaded) Loaded.Clear();
    }

    private static ShapeData? Cached(ICoreAPI api, string path)
    {
        lock (Loaded)
        {
            if (Loaded.TryGetValue((api.Side, path), out var data)) return data;

            var shape = Shape.TryGet(api, new AssetLocation(path));
            if (shape == null)
            {
                api.Logger.Warning("[nf3tstatue] Could not load {0}", path);
                return null;
            }

            var animations = (shape.Animations ?? []).GroupBy(a => a.Code).ToDictionary(g => g.Key, g => g.First());
            return Loaded[(api.Side, path)] = new ShapeData(shape, animations);
        }
    }

    private static ShapeData? Seraph(ICoreAPI api) => Cached(api, BodyShape);

    /// <summary>The animations are not needed for a static mesh, and cloning all of them is slow.</summary>
    private static Shape LightClone(ShapeData data)
    {
        lock (Loaded)
        {
            return new Shape
            {
                Elements = data.Shape.CloneElements(),
                Animations = [],
                TextureWidth = data.Shape.TextureWidth,
                TextureHeight = data.Shape.TextureHeight,
                Textures = new Dictionary<string, AssetLocation>(),
                TextureSizes = new Dictionary<string, int[]>(),
            };
        }
    }

    /// <summary>The player's emote animation (standing and riding have none).</summary>
    public static Animation? EmoteAnimation(ICoreAPI api, string pose) => Seraph(api)?.Animations.GetValueOrDefault(pose);

    /// <summary>The elk's gait animation, or null for an elk standing still.</summary>
    public static Animation? GaitAnimation(ICoreAPI api, string elkPose) =>
        StatuePoses.ElkAnimation(elkPose) is { } gait ? Cached(api, ElkShape)?.Animations.GetValueOrDefault(gait) : null;

    public static int FrameCount(ICoreAPI api, string pose) => EmoteAnimation(api, pose)?.QuantityFrames ?? 1;

    public static int GaitFrameCount(ICoreAPI api, string elkPose) => GaitAnimation(api, elkPose)?.QuantityFrames ?? 1;

    /// <summary>The frame where the emote peaks (for a bust, judged by the head and torso only).</summary>
    public static int PeakFrame(ICoreAPI api, string pose, bool bust)
    {
        var animation = EmoteAnimation(api, pose);
        return animation == null ? 0 : StatuePose.PeakFrame(animation, bust ? BustBones : null);
    }

    /// <summary>
    /// The best frame of the elk's stride: where the rider leans furthest into it, not where the legs move most.
    /// </summary>
    public static int GaitPeakFrame(ICoreAPI api, string elkPose) =>
        GaitAnimation(api, elkPose) != null
        && Seraph(api)?.Animations.GetValueOrDefault(StatuePoses.RiderAnimation(elkPose)) is { } ride
            ? StatuePose.PeakFrame(ride, RiderLeanBones)
            : 0;

    /// <summary>A real frame number: automatic (-1) becomes the peak frame, and others are kept within the animation.</summary>
    public static int ResolveFrame(ICoreAPI api, string pose, int frame, bool bust) =>
        frame < 0
            ? PeakFrame(api, pose, bust)
            : Math.Clamp(frame, 0, Math.Max(0, FrameCount(api, pose) - 1));

    public static int ResolveGaitFrame(ICoreAPI api, string elkPose, int frame) =>
        frame < 0
            ? GaitPeakFrame(api, elkPose)
            : Math.Clamp(frame, 0, Math.Max(0, GaitFrameCount(api, elkPose) - 1));

    public static MeshData? Build(ICoreClientAPI capi, ITesselatorAPI tesselator, Block block, string emote, int frame,
        StatueAppearance appearance, bool bust, float scale, float angleY)
    {
        var seraph = Seraph(capi);
        if (seraph == null) return null;

        var shape = LightClone(seraph);
        var mount = bust ? StatueAppearance.NoMount : appearance.Mount;
        var elkPose = appearance.ElkPose;

        foreach (var skinShape in appearance.SkinShapes) AddSkinPart(capi, shape, skinShape);
        foreach (var gear in appearance.Clothing) AddGear(capi, shape, gear);
        foreach (var gear in appearance.Armor) AddGear(capi, shape, gear);

        if (mount != StatueAppearance.NoMount)
        {
            // The elk's stride and the player's emote each have a frame of their own. While riding, the rider's own
            // animation has as many frames as the elk's gait, so the elk's frame serves for the rider as well.
            var gaitFrame = ResolveGaitFrame(capi, elkPose, appearance.ElkFrame);
            var emoteFrame = frame;

            var riding = emote == StatuePoses.Riding;
            var rideCode = riding ? StatuePoses.RiderAnimation(elkPose) : StatuePoses.RiderAnimation(StatuePoses.ElkStand);
            if (seraph.Animations.TryGetValue(rideCode, out var ride))
            {
                var seated = StatuePose.PoseAt(ride, riding ? Math.Clamp(gaitFrame, 0, ride.QuantityFrames - 1) : 0);

                // An emote takes over the arms and head completely: stacked on the riding pose (arms forward on the
                // reins) a wave would end up halfway between holding the reins and waving.
                if (!riding && seraph.Animations.TryGetValue(emote, out var seatedEmote))
                {
                    var keyFrame = emoteFrame < 0 ? StatuePose.PickPeakKeyFrame(seatedEmote) : StatuePose.PoseAt(seatedEmote, emoteFrame);
                    if (keyFrame != null) seated = StatuePose.Replace(seated, StatuePose.Only(keyFrame, RiderEmoteBones));
                }

                StatuePose.Apply(shape, seated);
            }

        }
        else if (seraph.Animations.TryGetValue(emote, out var animation))
        {
            // The animation is chosen by emote code, which is the shape animation's name for every vanilla emote.
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

        if (mount != StatueAppearance.NoMount) mesh = SeatOnElk(capi, tesselator, block, mesh, mount, appearance, elkPose, frame);

        var center = new Vec3f(0.5f, 0, 0.5f);
        if (bust || mount != StatueAppearance.NoMount) CenterOnPedestal(mesh);
        mesh.Scale(center, scale, scale, scale);
        mesh.Translate(0, PedestalHeight, 0);

        // A mount is much wider than the block's own pedestal, so it stands on a slab sized to its feet.
        if (mount != StatueAppearance.NoMount)
        {
            var platform = PlatformUnder(capi, tesselator, block, mesh);
            if (platform != null) mesh.AddMeshData(platform);
        }

        if (StatueMaterial.IsShiny(block.Variant["metal"])) MakeReflective(mesh);

        mesh.Rotate(center, 0, angleY, 0);
        return mesh;
    }

    /// <summary>Flags every vertex as reflective, which is what makes the game's metal blocks shine.</summary>
    private static void MakeReflective(MeshData mesh)
    {
        if (mesh.Flags == null) return;
        for (var i = 0; i < mesh.VerticesCount; i++) mesh.Flags[i] |= VertexFlags.ReflectiveBitMask;
    }

    /// <summary>A thin stone slab, level with the pedestal, under the lowest part of the mesh plus a margin.</summary>
    private static MeshData? PlatformUnder(ICoreClientAPI capi, ITesselatorAPI tesselator, Block block, MeshData mesh)
    {
        if (StatuePlatform.Footprint(mesh.xyz, mesh.VerticesCount) is not { } feet) return null;

        var face = new { texture = "#stone", uv = new[] { 0, 0, 16, 16 } };
        var json = JsonConvert.SerializeObject(new
        {
            textureWidth = 16,
            textureHeight = 16,
            textures = new Dictionary<string, string> { ["stone"] = "block/stone/rock/granite1" },
            elements = new[]
            {
                new
                {
                    name = "Platform",
                    from = new[] { feet.MinX * 16 - StatuePlatform.MarginPixels, 0, feet.MinZ * 16 - StatuePlatform.MarginPixels },
                    to = new[] { feet.MaxX * 16 + StatuePlatform.MarginPixels, PedestalHeight * 16, feet.MaxZ * 16 + StatuePlatform.MarginPixels },
                    faces = new { north = face, east = face, south = face, west = face, up = face, down = face },
                },
            },
        });

        var shape = JsonConvert.DeserializeObject<Shape>(json)!;
        shape.ResolveReferences(capi.Logger, "nf3tstatue-platform");
        StatuePose.UseSingleTile(shape);
        tesselator.TesselateShape(new TesselationMetaData
        {
            TypeForLogging = "statue-platform",
            TexSource = new StoneTextureSource(capi, block),
        }, shape, out var platform);
        return platform;
    }

    /// <summary>
    /// Sits the rider on a stone elk: the elk's seat attachment point and the rider offset the game uses for elks.
    /// </summary>
    private static MeshData SeatOnElk(ICoreClientAPI capi, ITesselatorAPI tesselator, Block block, MeshData rider,
        string mount, StatueAppearance appearance, string elkPose, int gaitFrame)
    {
        var elk = Cached(capi, ElkShape);
        if (elk == null) return rider;

        var shape = LightClone(elk);
        if (mount == StatueAppearance.ElkBull)
        {
            // Which antlers a bull has depends on the statue, so different statues are not all alike.
            var antlers = 1 + StableHash(string.Join(';', appearance.SkinShapes)) % 8;
            AddElkPart(capi, shape, "malefringe");
            AddElkPart(capi, shape, $"antlers0{antlers}");
        }

        PoseElk(elk, shape, mount == StatueAppearance.ElkBull, elkPose, gaitFrame);
        StatuePose.UseSingleTile(shape);
        tesselator.TesselateShape(new TesselationMetaData
        {
            TypeForLogging = "statue-elk",
            TexSource = new StoneTextureSource(capi, block),
        }, shape, out var elkMesh);

        var seat = StatuePose.AnchorMatrix(shape, "Midsection", "Rider");
        if (seat == null)
        {
            capi.Logger.Warning("[nf3tstatue] The elk shape has no Rider seat");
            return rider;
        }

        // The seraph and the elk both face -X in their models, so the rider needs no turn of their own.
        Mat4f.Translate(seat, seat, 0, RiderOffsetY, 0);
        Mat4f.Translate(seat, seat, -0.5f, 0, -0.5f);

        elkMesh.AddMeshData(rider.MatrixTransform(seat));
        return elkMesh;
    }

    /// <summary>Freezes the elk in its gait for the pose. A bull uses the gait that goes with its antlers.</summary>
    private static void PoseElk(ShapeData elk, Shape shape, bool bull, string elkPose, int frame)
    {
        var gait = StatuePoses.ElkAnimation(elkPose);
        if (gait == null) return;

        if (!(bull && elk.Animations.TryGetValue(gait + "-antlers", out var animation)) && !elk.Animations.TryGetValue(gait, out animation))
            return;

        var at = frame < 0 ? StatuePose.PeakFrame(animation) : Math.Clamp(frame, 0, animation.QuantityFrames - 1);
        StatuePose.Apply(shape, StatuePose.PoseAt(animation, at));
    }

    private static void AddElkPart(ICoreClientAPI capi, Shape elk, string name)
    {
        var location = new AssetLocation(ElkBullPartsPath + name + ".json");
        var part = Shape.TryGet(capi, location);
        if (part == null)
        {
            capi.Logger.Warning("[nf3tstatue] Elk part {0} not found", location);
            return;
        }

        part.SubclassForStepParenting("elkpart-");
        elk.StepParentShape(part, location.ToShortString(), ElkShape, capi.Logger, (_, _) => { });
    }

    /// <summary>string.GetHashCode differs between runs; this does not.</summary>
    private static int StableHash(string text) => text.Aggregate(17, (hash, c) => unchecked(hash * 31 + c)) & int.MaxValue;

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
        // Each hand has its own transform for the item; the left one falls back on the right one's, as in the game.
        var transform = (point == "LeftHand" ? collectible.TpOffHandTransform : null) ?? collectible.TpHandTransform
            ?? (collectible is Block ? ModelTransform.BlockDefaultTp() : ModelTransform.ItemDefaultTp());
        var matrix = StatuePose.HeldItemMatrix(body, anchor, point, transform);
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
