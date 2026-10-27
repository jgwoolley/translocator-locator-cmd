using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Statue;

/// <summary>
/// Freezes an animation keyframe into a shape. Pure shape manipulation, so it needs no client API.
/// </summary>
public static class StatuePose
{
    private const int FaceSize = 16;

    /// <summary>
    /// The keyframe where the emote peaks: the one (other than the rest frame 0) with the largest total rotation,
    /// counting only <paramref name="bones"/> when given.
    /// </summary>
    public static AnimationKeyFrame? PickPeakKeyFrame(Animation animation, ISet<string>? bones = null)
    {
        if (animation.KeyFrames == null || animation.KeyFrames.Length == 0) return null;

        var candidates = animation.KeyFrames.Where(k => k.Frame > 0).ToArray();
        if (candidates.Length == 0) candidates = animation.KeyFrames;

        return candidates.MaxBy(k => Magnitude(k, bones));
    }

    /// <summary>A copy of the keyframe that only moves <paramref name="bones"/>.</summary>
    public static AnimationKeyFrame Only(AnimationKeyFrame keyFrame, ISet<string> bones) => new()
    {
        Frame = keyFrame.Frame,
        Elements = keyFrame.Elements.Where(e => bones.Contains(e.Key)).ToDictionary(e => e.Key, e => e.Value),
    };

    /// <summary>
    /// The first pose with the bones of the second one swapped in: for those bones the second pose is used whole, so
    /// nothing of the first is left on them.
    /// </summary>
    public static AnimationKeyFrame Replace(AnimationKeyFrame pose, AnimationKeyFrame over)
    {
        var elements = new Dictionary<string, AnimationKeyFrameElement>(pose.Elements);
        foreach (var (name, element) in over.Elements) elements[name] = element;
        return new AnimationKeyFrame { Frame = pose.Frame, Elements = elements };
    }

    /// <summary>The frame number of <see cref="PickPeakKeyFrame"/>.</summary>
    public static int PeakFrame(Animation animation, ISet<string>? bones = null) =>
        PickPeakKeyFrame(animation, bones)?.Frame ?? 0;

    /// <summary>
    /// The pose at any frame. Like the engine, each element is interpolated between its own neighbouring keyframes
    /// and holds its first or last value outside of them.
    /// </summary>
    public static AnimationKeyFrame PoseAt(Animation animation, int frame)
    {
        var pose = new AnimationKeyFrame { Frame = frame, Elements = new Dictionary<string, AnimationKeyFrameElement>() };
        var keyFrames = animation.KeyFrames ?? [];

        foreach (var name in keyFrames.SelectMany(k => k.Elements.Keys).Distinct())
        {
            var owners = keyFrames.Where(k => k.Elements.ContainsKey(name)).OrderBy(k => k.Frame).ToArray();
            var before = owners.LastOrDefault(k => k.Frame <= frame);
            var after = owners.FirstOrDefault(k => k.Frame >= frame);
            var from = (before ?? after)!;
            var to = (after ?? before)!;
            var t = from.Frame == to.Frame ? 0 : (frame - from.Frame) / (double)(to.Frame - from.Frame);
            pose.Elements[name] = Lerp(from.Elements[name], to.Elements[name], t);
        }

        return pose;
    }

    private static AnimationKeyFrameElement Lerp(AnimationKeyFrameElement a, AnimationKeyFrameElement b, double t) => new()
    {
        OffsetX = Lerp(a.OffsetX, b.OffsetX, t),
        OffsetY = Lerp(a.OffsetY, b.OffsetY, t),
        OffsetZ = Lerp(a.OffsetZ, b.OffsetZ, t),
        RotationX = Lerp(a.RotationX, b.RotationX, t),
        RotationY = Lerp(a.RotationY, b.RotationY, t),
        RotationZ = Lerp(a.RotationZ, b.RotationZ, t),
    };

    private static double? Lerp(double? a, double? b, double t) =>
        a == null && b == null ? null : (a ?? 0) + ((b ?? 0) - (a ?? 0)) * t;

    private static double Magnitude(AnimationKeyFrame keyFrame, ISet<string>? bones)
    {
        double total = 0;
        foreach (var (name, element) in keyFrame.Elements)
        {
            if (bones != null && !bones.Contains(name)) continue;
            total += Math.Abs(element.RotationX ?? 0) + Math.Abs(element.RotationY ?? 0) +
                     Math.Abs(element.RotationZ ?? 0);
            total += 0.1 * (Math.Abs(element.OffsetX ?? 0) + Math.Abs(element.OffsetY ?? 0) +
                            Math.Abs(element.OffsetZ ?? 0));
        }

        return total;
    }

    /// <summary>
    /// Bakes the keyframe into the element rotations and positions. An element's position is relative to its
    /// parent, so children follow their parent without being touched.
    /// </summary>
    public static void Apply(Shape shape, AnimationKeyFrame keyFrame)
    {
        foreach (var root in shape.Elements) ApplyRecursive(root, keyFrame);
    }

    private static void ApplyRecursive(ShapeElement element, AnimationKeyFrame keyFrame)
    {
        if (element.Name != null && keyFrame.Elements.TryGetValue(element.Name, out var pose))
        {
            element.RotationX += pose.RotationX ?? 0;
            element.RotationY += pose.RotationY ?? 0;
            element.RotationZ += pose.RotationZ ?? 0;

            var dx = pose.OffsetX ?? 0;
            var dy = pose.OffsetY ?? 0;
            var dz = pose.OffsetZ ?? 0;
            Shift(element.From, dx, dy, dz);
            Shift(element.To, dx, dy, dz);
            Shift(element.RotationOrigin, dx, dy, dz);
        }

        if (element.Children == null) return;
        foreach (var child in element.Children) ApplyRecursive(child, keyFrame);
    }

    private static void Shift(double[]? v, double dx, double dy, double dz)
    {
        if (v == null || v.Length < 3) return;
        v[0] += dx;
        v[1] += dy;
        v[2] += dz;
    }

    /// <summary>
    /// Where a hand holds things: the transform of the named anchor element in model space (all its parents
    /// included, as posed), followed by the offset and rotation of the attachment point on it, applied as the game
    /// does it: the item is turned about its own middle.
    /// </summary>
    public static float[]? AnchorMatrix(Shape shape, string anchorName, string pointCode)
    {
        foreach (var root in shape.Elements)
        {
            var matrix = FindAnchor(root, anchorName, pointCode, Mat4f.Create());
            if (matrix != null) return matrix;
        }

        return null;
    }

    /// <summary>
    /// Where a held item goes, exactly as the game builds it for an item in a hand (ItemFishingPole's
    /// LoadHeldItemModelMatrix): the posed hand element, then the item's hand transform with the hand's attachment point
    /// folded in. Scale comes before the translation, so a transform's translation is in scaled units, and the attachment
    /// point's rotation adds to the transform's own rotation.
    /// </summary>
    public static float[]? HeldItemMatrix(Shape shape, string anchorName, string pointCode, ModelTransform transform)
    {
        var hand = AnchorMatrix(shape, anchorName, "no attachment point"); // the element's own pose, without the point
        if (hand == null) return null;

        var point = FindPoint(shape, anchorName, pointCode);
        var origin = transform.Origin;
        var scale = transform.ScaleXYZ;
        var translation = transform.Translation;
        var rotation = transform.Rotation;

        var matrix = (float[])hand.Clone();
        Mat4f.Translate(matrix, matrix, origin.X, origin.Y, origin.Z);
        Mat4f.Scale(matrix, matrix, scale.X, scale.Y, scale.Z);
        Mat4f.Translate(matrix, matrix,
            (float)((point?.PosX ?? 0) / 16 + translation.X),
            (float)((point?.PosY ?? 0) / 16 + translation.Y),
            (float)((point?.PosZ ?? 0) / 16 + translation.Z));
        Mat4f.RotateX(matrix, matrix, (float)(((point?.RotationX ?? 0) + rotation.X) * GameMath.DEG2RAD));
        Mat4f.RotateY(matrix, matrix, (float)(((point?.RotationY ?? 0) + rotation.Y) * GameMath.DEG2RAD));
        Mat4f.RotateZ(matrix, matrix, (float)(((point?.RotationZ ?? 0) + rotation.Z) * GameMath.DEG2RAD));
        Mat4f.Translate(matrix, matrix, -origin.X, -origin.Y, -origin.Z);
        return matrix;
    }

    private static AttachmentPoint? FindPoint(Shape shape, string anchorName, string pointCode)
    {
        AttachmentPoint? found = null;
        foreach (var root in shape.Elements) found ??= FindPoint(root, anchorName, pointCode);
        return found;
    }

    private static AttachmentPoint? FindPoint(ShapeElement element, string anchorName, string pointCode)
    {
        if (element.Name == anchorName) return element.AttachmentPoints?.FirstOrDefault(p => p.Code == pointCode);
        if (element.Children == null) return null;

        AttachmentPoint? found = null;
        foreach (var child in element.Children) found ??= FindPoint(child, anchorName, pointCode);
        return found;
    }

    private static float[]? FindAnchor(ShapeElement element, string anchorName, string pointCode, float[] parent)
    {
        var world = Mat4f.Mul(Mat4f.Create(), parent, element.GetLocalTransformMatrix(0));

        if (element.Name == anchorName)
        {
            var point = element.AttachmentPoints?.FirstOrDefault(p => p.Code == pointCode);
            if (point != null)
            {
                // The game's own order (AttachmentPointAndPose.Mul): move to the point, then turn about the middle of
                // the attached thing (a unit cube), not about its corner.
                Mat4f.Translate(world, world, (float)point.PosX / 16, (float)point.PosY / 16, (float)point.PosZ / 16);
                Mat4f.Translate(world, world, -0.5f, -0.5f, -0.5f);
                Mat4f.RotateX(world, world, (float)(point.RotationX * GameMath.DEG2RAD));
                Mat4f.RotateY(world, world, (float)(point.RotationY * GameMath.DEG2RAD));
                Mat4f.RotateZ(world, world, (float)(point.RotationZ * GameMath.DEG2RAD));
                Mat4f.Translate(world, world, 0.5f, 0.5f, 0.5f);
            }

            return world;
        }

        if (element.Children == null) return null;
        foreach (var child in element.Children)
        {
            var found = FindAnchor(child, anchorName, pointCode, world);
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>
    /// The names of the element called <paramref name="name"/> and everything below it (including step-parented
    /// skin parts), or an empty set if there is no such element.
    /// </summary>
    public static HashSet<string> SubtreeNames(Shape shape, string name)
    {
        var names = new HashSet<string>();
        foreach (var root in shape.Elements) Collect(root, name, false, names);
        return names;
    }

    private static void Collect(ShapeElement element, string name, bool inside, HashSet<string> names)
    {
        inside |= element.Name == name;
        if (inside && element.Name != null) names.Add(element.Name);
        if (element.Children == null) return;
        foreach (var child in element.Children) Collect(child, name, inside, names);
    }

    /// <summary>
    /// Removes the faces of every element not in <paramref name="keep"/>. The elements stay in the tree, so the
    /// transforms of the hidden ancestors still apply to the ones that remain.
    /// </summary>
    public static void HideExcept(Shape shape, ISet<string> keep)
    {
        foreach (var root in shape.Elements) HideExcept(root, keep);
    }

    private static void HideExcept(ShapeElement element, ISet<string> keep)
    {
        if (element.Name == null || !keep.Contains(element.Name))
            element.FacesResolved = new ShapeElementFace[6];

        if (element.Children == null) return;
        foreach (var child in element.Children) HideExcept(child, keep);
    }

    /// <summary>
    /// The seraph uvs point into a skin atlas. Point every face at the start of a single material tile instead,
    /// sized by the face so the stone grain keeps a constant scale.
    /// </summary>
    public static void UseSingleTile(Shape shape)
    {
        shape.TextureWidth = FaceSize;
        shape.TextureHeight = FaceSize;
        shape.TextureSizes = new Dictionary<string, int[]>();
        foreach (var root in shape.Elements) UseSingleTile(root);
    }

    private static void UseSingleTile(ShapeElement element)
    {
        // Loaded shapes only keep FacesResolved, in BlockFacing order: north, east, south, west, up, down.
        if (element.FacesResolved != null && element.From != null && element.To != null)
        {
            var sx = Clamp(Math.Abs(element.To[0] - element.From[0]));
            var sy = Clamp(Math.Abs(element.To[1] - element.From[1]));
            var sz = Clamp(Math.Abs(element.To[2] - element.From[2]));
            float[][] uvs = [[0, 0, sx, sy], [0, 0, sz, sy], [0, 0, sx, sy], [0, 0, sz, sy], [0, 0, sx, sz], [0, 0, sx, sz]];

            for (var i = 0; i < element.FacesResolved.Length; i++)
            {
                var face = element.FacesResolved[i];
                if (face != null) face.Uv = (float[])uvs[i].Clone();
            }
        }

        if (element.Children == null) return;
        foreach (var child in element.Children) UseSingleTile(child);
    }

    private static float Clamp(double v) => (float)Math.Clamp(v, 1, FaceSize);
}
