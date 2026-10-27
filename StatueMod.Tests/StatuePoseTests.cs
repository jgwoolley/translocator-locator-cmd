using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
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

    private static string ElkPath =>
        Path.Combine(AppContext.BaseDirectory, "../../../../vs_server/assets/survival/shapes/entity/animal/mammal/hooved/deer/elk/female.json");

    private static Shape LoadElk()
    {
        var shape = JsonConvert.DeserializeObject<Shape>(File.ReadAllText(ElkPath))!;
        foreach (var element in shape.Elements) element.TrimTextureNamesAndResolveFaces();
        return shape;
    }

    [Fact]
    public void TheElkHasARiderSeatAboveItsBack()
    {
        var seat = StatuePose.AnchorMatrix(LoadElk(), "Midsection", "Rider");

        Assert.NotNull(seat);
        // The seat is 11 model pixels above the Midsection's origin, which itself sits at the elk's back.
        Assert.True(seat![13] > 11 / 16f, $"seat height {seat[13]} should be above the elk's back");
    }

    [Fact]
    public void TheRidingPoseBendsTheLegsAndLeavesTheEmoteBonesToTheEmote()
    {
        var ride = LoadSeraph().Animations.First(a => a.Code == "rideelk-idle");
        var pose = StatuePose.PoseAt(ride, 0);

        Assert.Equal(-72, pose.Elements["UpperFootL"].RotationZ);
        Assert.Equal(80, pose.Elements["LowerFootL"].RotationZ);
    }

    [Fact]
    public void OnlyKeepsTheListedBones()
    {
        var wave = LoadSeraph().Animations.First(a => a.Code == "wave");
        var peak = StatuePose.PickPeakKeyFrame(wave)!;

        var filtered = StatuePose.Only(peak, new HashSet<string> { "UpperArmR", "Head" });

        Assert.All(filtered.Elements.Keys, name => Assert.Contains(name, new[] { "UpperArmR", "Head" }));
        Assert.Contains("UpperArmR", filtered.Elements.Keys);
        Assert.Equal(peak.Frame, filtered.Frame);
    }

    [Theory]
    [InlineData("", false, true)]
    [InlineData("elkcow", false, true)]
    [InlineData("elkbull", false, true)]
    [InlineData("horse", false, false)]
    [InlineData("elkcow", true, false)]
    [InlineData("", true, true)]
    public void OnlyKnownMountsAreAcceptedAndABustCannotRide(string mount, bool bust, bool expected)
    {
        Assert.Equal(expected, StatueAppearance.IsValidMount(mount, bust));
    }

    [Theory]
    [InlineData("stand", null, "rideelk-idle")]
    [InlineData("walk", "walk", "rideelk-walk")]
    [InlineData("gallop", "run", "rideelk-run")]
    public void EachElkPoseNamesAnElkGaitAndTheRiderAnimationThatGoesWithIt(string elkPose, string? gait, string rider)
    {
        Assert.True(StatuePoses.IsKnownElkPose(elkPose));
        Assert.Equal(gait, StatuePoses.ElkAnimation(elkPose));
        Assert.Equal(rider, StatuePoses.RiderAnimation(elkPose));
    }

    [Theory]
    [InlineData("none", true)]
    [InlineData("riding", true)]
    [InlineData("wave", false)]
    public void StandingAndRidingAreThePlayerPosesBesidesTheGameEmotes(string pose, bool expected)
    {
        Assert.Equal(expected, StatuePoses.IsKnown(pose));
    }

    [Fact]
    public void AnUnknownElkPoseIsRejected()
    {
        Assert.False(StatuePoses.IsKnownElkPose("flying"));
    }

    [Theory]
    [InlineData("walk")]
    [InlineData("gallop")]
    public void TheElkGaitAndItsRiderHaveTheSameFramesSoOneFrameNumberServesBoth(string elkPose)
    {
        var seraph = LoadSeraph().Animations;
        var elk = LoadElk().Animations;

        var rider = seraph.First(a => a.Code == StatuePoses.RiderAnimation(elkPose));
        var gait = elk.First(a => a.Code == StatuePoses.ElkAnimation(elkPose));
        var gaitWithAntlers = elk.First(a => a.Code == StatuePoses.ElkAnimation(elkPose) + "-antlers");

        Assert.Equal(rider.QuantityFrames, gait.QuantityFrames);
        Assert.Equal(gait.QuantityFrames, gaitWithAntlers.QuantityFrames);
    }

    [Fact]
    public void AGallopingElkHasItsBodyMovedFromRest()
    {
        var shape = LoadElk();
        var run = shape.Animations.First(a => a.Code == StatuePoses.ElkAnimation("gallop"));

        var before = StatuePose.AnchorMatrix(shape, "Midsection", "Rider")![13];
        StatuePose.Apply(shape, StatuePose.PoseAt(run, StatuePose.PeakFrame(run)));
        var after = StatuePose.AnchorMatrix(shape, "Midsection", "Rider")![13];

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void ReplacingBonesLeavesNothingOfTheOldPoseOnThemAndKeepsTheOthers()
    {
        var riding = new AnimationKeyFrame
        {
            Elements = new()
            {
                ["UpperArmR"] = new AnimationKeyFrameElement { RotationX = 14, RotationY = 14, RotationZ = -17 },
                ["UpperTorso"] = new AnimationKeyFrameElement { RotationZ = 8 },
            },
        };
        var wave = new AnimationKeyFrame
        {
            Elements = new() { ["UpperArmR"] = new AnimationKeyFrameElement { RotationZ = -97 } },
        };

        var merged = StatuePose.Replace(riding, wave);

        Assert.Equal(-97, merged.Elements["UpperArmR"].RotationZ);
        Assert.Null(merged.Elements["UpperArmR"].RotationX);
        Assert.Equal(8, merged.Elements["UpperTorso"].RotationZ);
        // The original pose is not changed.
        Assert.Equal(14, riding.Elements["UpperArmR"].RotationX);
    }

    [Fact]
    public void AWavingRiderRaisesTheRightArmHigherThanARiderHoldingTheReins()
    {
        var shape = LoadSeraph();
        var ride = shape.Animations.First(a => a.Code == "rideelk-idle");
        var wave = shape.Animations.First(a => a.Code == "wave");
        var seated = StatuePose.PoseAt(ride, 0);
        var waving = StatuePose.Replace(seated, StatuePose.Only(StatuePose.PickPeakKeyFrame(wave)!, new HashSet<string> { "UpperArmR", "LowerArmR" }));

        var reins = LoadSeraph();
        StatuePose.Apply(reins, seated);
        var raised = LoadSeraph();
        StatuePose.Apply(raised, waving);

        var handOnReins = StatuePose.AnchorMatrix(reins, "ItemAnchor", "RightHand")![13];
        var handWaving = StatuePose.AnchorMatrix(raised, "ItemAnchor", "RightHand")![13];
        Assert.True(handWaving > handOnReins, $"waving hand y {handWaving} should be above the reins {handOnReins}");
    }

    [Fact]
    public void TheHandTurnsAHeldItemAboutItsMiddleAsTheGameDoes()
    {
        var shape = LoadSeraph();
        var withPoint = StatuePose.AnchorMatrix(shape, "ItemAnchor", "RightHand")!;
        // A point code the anchor does not have: the anchor's own transform, with no attachment point applied.
        var withoutPoint = StatuePose.AnchorMatrix(shape, "ItemAnchor", "NoSuchPoint")!;

        // The hand's attachment point turns the item half a turn about Y. The game does that about a vertical axis
        // through (-0.5, -0.5) of the item, so a point on that axis stays put and one on the item's origin does not.
        float[] onAxis = [-0.5f, 0.3f, -0.5f, 1];
        float[] atOrigin = [0f, 0.3f, 0f, 1];

        Assert.Equal(Mat4f.MulWithVec4(withoutPoint, onAxis), Mat4f.MulWithVec4(withPoint, onAxis), new FloatArrayComparer());
        Assert.NotEqual(Mat4f.MulWithVec4(withoutPoint, atOrigin), Mat4f.MulWithVec4(withPoint, atOrigin), new FloatArrayComparer());
    }

    private class FloatArrayComparer : IEqualityComparer<float[]>
    {
        public bool Equals(float[]? a, float[]? b) => a != null && b != null && a.Zip(b).All(p => Math.Abs(p.First - p.Second) < 1e-4);
        public int GetHashCode(float[] obj) => 0;
    }

    [Fact]
    public void AnItemWithTheGamesDefaultHandTransformEndsUpWithItsMiddleInTheHand()
    {
        var shape = LoadSeraph();
        var hand = StatuePose.AnchorMatrix(shape, "ItemAnchor", "no attachment point")!;
        var handPosition = new[] { hand[12], hand[13], hand[14] };

        var matrix = StatuePose.HeldItemMatrix(shape, "ItemAnchor", "RightHand", ModelTransform.ItemDefaultTp())!;
        var middle = Mat4f.MulWithVec4(matrix, [0.5f, 0.5f, 0.5f, 1]);

        // A held item is shown with its middle in the hand: this is what the game's default transforms are made for.
        var distance = Math.Sqrt(Enumerable.Range(0, 3).Sum(i => Math.Pow(middle[i] - handPosition[i], 2)));
        Assert.True(distance < 0.1, $"the item's middle is {distance:F3} blocks from the hand");
    }

    [Fact]
    public void ABlockWithTheGamesDefaultHandTransformEndsUpInTheHandToo()
    {
        var shape = LoadSeraph();
        var hand = StatuePose.AnchorMatrix(shape, "ItemAnchor", "no attachment point")!;

        var matrix = StatuePose.HeldItemMatrix(shape, "ItemAnchor", "RightHand", ModelTransform.BlockDefaultTp())!;
        var middle = Mat4f.MulWithVec4(matrix, [0.5f, 0.5f, 0.5f, 1]);

        var distance = Math.Sqrt(Enumerable.Range(0, 3).Sum(i => Math.Pow(middle[i] - hand[12 + i], 2)));
        Assert.True(distance < 0.2, $"the block's middle is {distance:F3} blocks from the hand");
    }

    [Fact]
    public void AMissingHandGivesNoMatrix()
    {
        Assert.Null(StatuePose.HeldItemMatrix(LoadSeraph(), "NoSuchHand", "RightHand", ModelTransform.ItemDefaultTp()));
    }
}
