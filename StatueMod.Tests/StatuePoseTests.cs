using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Xunit;

namespace Nf3t.VintageStory.Statue.Tests;

public class StatuePoseTests
{
    private static string SeraphPath =>
        Path.Combine(AppContext.BaseDirectory, "../../../../vs_server/assets/game/shapes/entity/humanoid/seraph-faceless.json");

    private static Shape LoadSeraph()
    {
        var shape = JsonConvert.DeserializeObject<Shape>(File.ReadAllText(SeraphPath))!;
        foreach (var element in shape.Elements) element.TrimTextureNamesAndResolveFaces();
        return shape;
    }

    private static ShapeElement Find(ShapeElement[] elements, string name) =>
        elements.Select(e => e.Name == name ? e : e.Children == null ? null : Find(e.Children, name))
            .FirstOrDefault(e => e != null)!;

    [Fact]
    public void PeakKeyFrameOfWaveIsNotTheRestFrame()
    {
        var wave = LoadSeraph().Animations.First(a => a.Code == "wave");
        var peak = StatuePose.PickPeakKeyFrame(wave)!;
        Assert.True(peak.Frame > 0);
    }

    [Fact]
    public void ApplyingWaveRotatesTheRightArm()
    {
        var shape = LoadSeraph();
        var arm = Find(shape.Elements, "UpperArmR");
        var before = (arm.RotationX, arm.RotationY, arm.RotationZ);

        var wave = shape.Animations.First(a => a.Code == "wave");
        StatuePose.Apply(shape, StatuePose.PickPeakKeyFrame(wave)!);

        Assert.NotEqual(before, (arm.RotationX, arm.RotationY, arm.RotationZ));
    }

    private static Animation TwoKeyFrameAnimation() => new()
    {
        QuantityFrames = 40,
        KeyFrames =
        [
            new AnimationKeyFrame { Frame = 0, Elements = new() { ["Head"] = new AnimationKeyFrameElement { RotationX = 0, OffsetY = 0 } } },
            new AnimationKeyFrame { Frame = 20, Elements = new() { ["Head"] = new AnimationKeyFrameElement { RotationX = 40, OffsetY = 2 } } },
        ],
    };

    [Fact]
    public void PoseAtAKeyFrameIsThatKeyFrame()
    {
        var pose = StatuePose.PoseAt(TwoKeyFrameAnimation(), 20);
        Assert.Equal(40, pose.Elements["Head"].RotationX);
        Assert.Equal(2, pose.Elements["Head"].OffsetY);
    }

    [Fact]
    public void PoseBetweenKeyFramesIsInterpolated()
    {
        var pose = StatuePose.PoseAt(TwoKeyFrameAnimation(), 5);
        Assert.Equal(10, pose.Elements["Head"].RotationX);
        Assert.Equal(0.5, pose.Elements["Head"].OffsetY);
    }

    [Fact]
    public void PoseAfterTheLastKeyFrameHoldsIt()
    {
        Assert.Equal(40, StatuePose.PoseAt(TwoKeyFrameAnimation(), 35).Elements["Head"].RotationX);
    }

    [Fact]
    public void PeakFrameIsAFrameNumberNotMinusOne()
    {
        Assert.Equal(20, StatuePose.PeakFrame(TwoKeyFrameAnimation()));
    }

    [Fact]
    public void PoseAtAWaveKeyFrameMatchesTheShape()
    {
        var wave = LoadSeraph().Animations.First(a => a.Code == "wave");
        Assert.Equal(StatuePose.PickPeakKeyFrame(wave)!.Frame, StatuePose.PeakFrame(wave));
        Assert.Equal(wave.KeyFrames.First(k => k.Frame == 41).Elements["UpperArmR"].RotationX,
            StatuePose.PoseAt(wave, 41).Elements["UpperArmR"].RotationX);
    }

    [Fact]
    public void OffsetsMoveOnlyTheElementBecauseChildrenArePositionedRelativeToIt()
    {
        var parent = new ShapeElement
        {
            Name = "P", From = [0, 0, 0], To = [1, 1, 1], RotationOrigin = [0, 0, 0],
            Children = [new ShapeElement { Name = "C", From = [2, 2, 2], To = [3, 3, 3] }],
        };
        var shape = new Shape { Elements = [parent] };
        var keyFrame = new AnimationKeyFrame
        {
            Elements = new() { ["P"] = new AnimationKeyFrameElement { OffsetY = 4 } },
        };

        StatuePose.Apply(shape, keyFrame);

        Assert.Equal(4, parent.From![1]);
        Assert.Equal(4, parent.RotationOrigin![1]);
        Assert.Equal(2, parent.Children![0].From![1]);
    }

    [Fact]
    public void BustKeepsOnlyTheNeckAndItsDescendantsVisible()
    {
        var shape = LoadSeraph();
        var keep = StatuePose.SubtreeNames(shape, "Neck");
        Assert.Contains("Head", keep);
        Assert.DoesNotContain("UpperTorso", keep);

        StatuePose.HideExcept(shape, keep);

        var torso = Find(shape.Elements, "UpperTorso");
        var head = Find(shape.Elements, "Head");
        Assert.All(torso.FacesResolved!, f => Assert.Null(f));
        Assert.Contains(head.FacesResolved!, f => f != null);
    }

    [Fact]
    public void BoneFilterIgnoresMotionOutsideTheGivenBones()
    {
        var animation = new Animation
        {
            KeyFrames =
            [
                new AnimationKeyFrame { Frame = 10, Elements = new() { ["UpperArmR"] = new AnimationKeyFrameElement { RotationX = 90 } } },
                new AnimationKeyFrame { Frame = 20, Elements = new() { ["Head"] = new AnimationKeyFrameElement { RotationX = 10 } } },
            ],
        };

        Assert.Equal(10, StatuePose.PickPeakKeyFrame(animation)!.Frame);
        Assert.Equal(20, StatuePose.PickPeakKeyFrame(animation, new HashSet<string> { "Head" })!.Frame);
    }

    [Fact]
    public void SingleTileUsesFaceSizedUvs()
    {
        var element = new ShapeElement { Name = "E", From = [0, 0, 0], To = [4, 6, 2] };
        element.FacesResolved![0] = new ShapeElementFace { Uv = [9, 9, 9, 9] };
        element.FacesResolved![4] = new ShapeElementFace { Uv = [9, 9, 9, 9] };

        StatuePose.UseSingleTile(new Shape { Elements = [element] });

        Assert.Equal([0f, 0f, 4f, 6f], element.FacesResolved![0]!.Uv);
        Assert.Equal([0f, 0f, 4f, 2f], element.FacesResolved![4]!.Uv);
    }

    [Fact]
    public void BothHandAnchorsAreFound()
    {
        var shape = LoadSeraph();
        Assert.NotNull(StatuePose.AnchorMatrix(shape, "ItemAnchor", "RightHand"));
        Assert.NotNull(StatuePose.AnchorMatrix(shape, "ItemAnchorL", "LeftHand"));
        Assert.Null(StatuePose.AnchorMatrix(shape, "NoSuchAnchor", "RightHand"));
    }

    [Fact]
    public void WavingLiftsTheRightHandHigherThanRest()
    {
        var shape = LoadSeraph();
        var rest = StatuePose.AnchorMatrix(shape, "ItemAnchor", "RightHand")![13];

        var wave = shape.Animations.First(a => a.Code == "wave");
        StatuePose.Apply(shape, StatuePose.PickPeakKeyFrame(wave)!);
        var waving = StatuePose.AnchorMatrix(shape, "ItemAnchor", "RightHand")![13];

        Assert.True(waving > rest, $"waving hand y {waving} should be above resting {rest}");
    }
}
