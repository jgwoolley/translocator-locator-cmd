using Vintagestory.API.Datastructures;
using Xunit;

namespace Nf3t.VintageStory.Statue.Tests;

public class StatueAppearanceTests
{
    [Fact]
    public void TheElkSettingsSurviveBeingSavedAndLoaded()
    {
        var tree = new TreeAttribute();
        new StatueAppearance { Mount = StatueAppearance.ElkBull, ElkPose = StatuePoses.ElkGallop, ElkFrame = 17 }.Write(tree);

        // Nothing here is an item, so no world is needed to resolve one.
        var loaded = StatueAppearance.Read(tree, null!);

        Assert.Equal(StatueAppearance.ElkBull, loaded.Mount);
        Assert.Equal(StatuePoses.ElkGallop, loaded.ElkPose);
        Assert.Equal(17, loaded.ElkFrame);
    }

    [Fact]
    public void AStatueSavedBeforeTheElkExistedLoadsWithNoElkAndAutomaticFrames()
    {
        var loaded = StatueAppearance.Read(new TreeAttribute(), null!);

        Assert.Equal(StatueAppearance.NoMount, loaded.Mount);
        Assert.Equal(StatuePoses.ElkStand, loaded.ElkPose);
        Assert.Equal(-1, loaded.ElkFrame);
    }
}
