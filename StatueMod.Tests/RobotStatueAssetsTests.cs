using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Xunit;

namespace Nf3t.VintageStory.Statue.Tests;

/// <summary>The robot is a special statue type drawn from a fixed shape: the shape must be well formed and match its type entry.</summary>
public class RobotStatueAssetsTests
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "../../../../");
    private static string Mod(string path) => Path.Combine(Root, "StatueMod/assets/nf3tstatue", path);

    private static Shape LoadRobot() => JsonConvert.DeserializeObject<Shape>(File.ReadAllText(Mod("shapes/block/robot.json")))!;
    private static ShapeElement Find(string name) => LoadRobot().Elements.First(e => e.Name == name);
    private static double CentreZ(ShapeElement e) => (e.From![2] + e.To![2]) / 2;

    [Fact]
    public void EveryPartIsAWellFormedBoxWithAllSixFacesOnTheStoneTexture()
    {
        var shape = LoadRobot();
        Assert.True(shape.Elements.Length > 20, "the robot should have many parts");
        foreach (var part in shape.Elements)
        {
            for (var axis = 0; axis < 3; axis++)
                Assert.True(part.From![axis] <= part.To![axis], $"{part.Name} has from > to on axis {axis}");

            Assert.Equal(6, part.FacesResolved!.Count(f => f != null));
            Assert.All(part.FacesResolved!, f => Assert.Equal("stone", f!.Texture));
        }
    }

    [Fact]
    public void ItStandsOnAPedestalAtTheBottomOfTheBlockAndFitsTheBlocksFootprint()
    {
        var shape = LoadRobot();
        var pedestal = shape.Elements.First(e => e.Name == "Pedestal");
        Assert.Equal(0, pedestal.From![1]);

        foreach (var part in shape.Elements.Where(e => e.Name != "Pedestal"))
            Assert.True(part.From![1] >= pedestal.To![1] - 0.001, $"{part.Name} sinks into the pedestal");

        Assert.All(shape.Elements, part =>
        {
            Assert.InRange(part.From![0], 0, 16);
            Assert.InRange(part.To![0], 0, 16);
            Assert.InRange(part.From![2], 0, 16);
            Assert.InRange(part.To![2], 0, 16);
        });
    }

    [Fact]
    public void ItIsAboutTwoBlocksTall()
    {
        var top = LoadRobot().Elements.Max(e => e.To![1]);
        Assert.InRange(top, 28, 36);
    }

    [Fact]
    public void TheFrontFeaturesAreOnTheSouthSideWhichIsWhatTheOrientationRotationsAssume()
    {
        // The block types turn the model with rotateY 0/90/180/270 for north/east/south/west, which makes the south face the front.
        var head = CentreZ(Find("Head"));
        Assert.True(CentreZ(Find("EyeR")) > head);
        Assert.True(CentreZ(Find("EyeL")) > head);
        Assert.True(CentreZ(Find("ChestDial")) > CentreZ(Find("BodyMid")));
        Assert.True(CentreZ(Find("Plug")) > CentreZ(Find("Belt")));
        Assert.True(CentreZ(Find("YellowBand")) > CentreZ(Find("BodyLow")));
    }

    [Fact]
    public void ItHasTwoEyesTwoClawedArmsAndTwoLegs()
    {
        var names = LoadRobot().Elements.Select(e => e.Name!).ToArray();
        foreach (var side in new[] { "R", "L" })
        {
            Assert.Contains("Eye" + side, names);
            Assert.Contains("UpperArm" + side, names);
            Assert.Contains("Forearm" + side, names);
            Assert.Contains("Claw" + side + "0", names);
            Assert.Contains("Claw" + side + "1", names);
            Assert.Contains("UpperLeg" + side, names);
            Assert.Contains("Foot" + side, names);
        }
    }
}
