using Newtonsoft.Json.Linq;
using Vintagestory.API.Datastructures;
using Xunit;

namespace Nf3t.VintageStory.Statue.Tests;

/// <summary>The chisel-and-commit workflow: blank blocks, finished statues, molds, tack and the commit packet.</summary>
public class StatueWorkflowTests
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "../../../../");
    private static string Mod(string path) => Path.Combine(Root, "StatueMod/assets/nf3tstatue", path);
    private static JObject Json(string path) => JObject.Parse(File.ReadAllText(Mod(path)));
    private static string[] States(JObject type, string group) =>
        type["variantgroups"]!.First(g => (string?)g["code"] == group)["states"]!.Select(s => (string)s!).ToArray();

    [Fact]
    public void EveryFinishedStoneStatueHasABlankBlockOfTheSameSizeAndRock()
    {
        var blank = Json("blocktypes/statueblock.json");
        var statue = Json("blocktypes/statue.json");

        foreach (var group in new[] { "size", "rock" }) Assert.Equal(States(statue, group), States(blank, group));
        Assert.Equal("BlockStatueBlank", (string?)blank["class"]);
        Assert.False((bool)blank["attributes"]!["canChisel"]!, "the game's own chiseling must not take the block over");
    }

    [Fact]
    public void OnlyTheBlankBlocksCanBeTakenFromTheCreativeMenu()
    {
        Assert.NotNull(Json("blocktypes/statueblock.json")["creativeinventory"]);
        Assert.Null(Json("blocktypes/statue.json")["creativeinventory"]);
        Assert.Null(Json("blocktypes/statuemetal.json")["creativeinventory"]);
    }

    [Fact]
    public void TheCraftingRecipesMakeBlankBlocksNotFinishedStatues()
    {
        var recipes = JArray.Parse(File.ReadAllText(Mod("recipes/grid/statue.json")));
        Assert.NotEmpty(recipes);
        Assert.All(recipes, r => Assert.StartsWith("nf3tstatue:statueblock-", (string?)r["output"]!["code"]));
        Assert.False(File.Exists(Mod("recipes/grid/statuemetal.json")), "metal statues are cast, not crafted");
    }

    [Theory]
    [InlineData("bust", 5)]
    [InlineData("small", 8)]
    [InlineData("large", 16)]
    public void AMoldNeedsMoreClayForABiggerStatue(string size, int clay)
    {
        Assert.Equal(clay, StatueMolding.ClayNeeded(size));
        Assert.False(File.Exists(Mod("recipes/grid/statuemold.json")), "the mold is made on the statue, not in the crafting grid");
    }

    [Fact]
    public void TheStoneStatueListsTheFireClayTextureItIsPackedWith()
    {
        Assert.Equal("game:block/clay/fireclay", (string?)Json("blocktypes/statue.json")["textures"]!["clay"]!["base"]);
        Assert.True(File.Exists(Path.Combine(Root, "vintagestory/assets/survival/textures/block/clay/fireclay.png")));
        Assert.NotNull(Json("lang/en.json")["nf3tstatue:help-clay"]);
        Assert.NotNull(Json("lang/en.json")["nf3tstatue:help-take-mold"]);
    }

    [Fact]
    public void AMoldCastsTheMetalStatueOfItsSizeAndBreaksWhenFilled()
    {
        var mold = Json("blocktypes/statuemold.json");
        Assert.Equal("BlockStatueMold", (string?)mold["class"]);
        Assert.Equal("BlockEntityStatueMold", (string?)mold["entityClass"]);
        Assert.Equal(["fired"], States(mold, "materialtype"));

        foreach (var size in States(mold, "size"))
        {
            var attributes = mold["attributesByType"]![$"statuemold-{size}-*"]!;
            var expected = $"nf3tstatue:statuemetal-{size}-{{metal}}";
            Assert.Equal(expected, (string?)attributes["drop"]!["code"]);
            Assert.True((int)attributes["requiredUnits"]! > 0);
            Assert.True((bool)attributes["breaksWhenFilled"]!);
            Assert.NotEmpty(attributes["fillQuadsByLevel"]!);
        }
    }

    [Fact]
    public void EveryBlockAndMoldHasANameAndEveryTackTypeHasALabel()
    {
        var lang = Json("lang/en.json");
        foreach (var size in new[] { "small", "large", "bust" }) Assert.NotNull(lang[$"nf3tstatue:name-blank-{size}"]);
        foreach (var size in States(Json("blocktypes/statuemold.json"), "size")) Assert.NotNull(lang[$"nf3tstatue:block-statuemold-{size}-fired"]);

        for (var i = 0; i < StatueTack.PieceCount; i++)
        {
            var piece = (StatueTack.Piece)i;
            Assert.NotNull(lang["nf3tstatue:dialog-tack-" + piece.ToString().ToLowerInvariant()]);
            foreach (var type in StatueTack.Choices(piece)) Assert.NotNull(lang[StatueTack.LangKey(piece, type)]);
        }
    }

    [Fact]
    public void EveryTackShapeExistsInTheGame()
    {
        for (var i = 0; i < StatueTack.PieceCount; i++)
        {
            foreach (var type in StatueTack.Choices((StatueTack.Piece)i).Where(t => t != StatueTack.None))
            {
                var path = StatueTack.ShapePath((StatueTack.Piece)i, type)["game:".Length..];
                Assert.True(File.Exists(Path.Combine(Root, "vintagestory/assets/survival", path)), path);
            }
        }
    }

    [Fact]
    public void TheCommitPacketSurvivesTheNetwork()
    {
        var tack = StatueTack.Empty();
        tack[(int)StatueTack.Piece.Saddle] = "saddle1";
        var request = new EditRequest("Alice", "wave", 12, true, false, true, 3, StatueAppearance.HandFromCopy, 1.5f,
            StatueAppearance.ElkBull, StatuePoses.ElkWalk, 7, tack, 2, 6);

        var decoded = EditRequest.Decode(request.Encode())!;

        Assert.Equal(request with { Tack = decoded.Tack }, decoded);
        Assert.Equal(tack, decoded.Tack);
    }

    [Fact]
    public void AGarbledPacketIsRejectedInsteadOfCrashing()
    {
        Assert.Null(EditRequest.Decode([]));
        Assert.Null(EditRequest.Decode([1, 2, 3]));
    }

    [Fact]
    public void ATackNameThatTheGameDoesNotKnowIsRejected()
    {
        var tack = StatueTack.Empty();
        tack[(int)StatueTack.Piece.Bridle] = "../../secret";
        var bad = new EditRequest("A", "wave", 0, true, true, true, -1, -1, 0, StatueAppearance.ElkCow, StatuePoses.ElkStand, 0, tack,
            EditRequest.NoCopy);
        var good = bad with { Tack = StatueTack.Empty() };

        Assert.False(bad.HasValidMount(false));
        Assert.True(good.HasValidMount(false));
        Assert.False(good.HasValidMount(true) && good.Mount != StatueAppearance.NoMount, "a bust cannot ride");
    }

    [Fact]
    public void TackSurvivesBeingSavedAndUnknownTackIsDroppedOnLoad()
    {
        var appearance = new StatueAppearance { Mount = StatueAppearance.ElkCow };
        appearance.Tack[(int)StatueTack.Piece.Face] = "face16";
        var tree = new TreeAttribute();
        appearance.Write(tree);
        Assert.Equal("face16", StatueAppearance.Read(tree, null!).Tack[(int)StatueTack.Piece.Face]);

        tree.SetString("tack", "nonsense;face99;saddle1;saddlebags9");
        var loaded = StatueAppearance.Read(tree, null!);
        Assert.Equal([StatueTack.None, StatueTack.None, "saddle1", StatueTack.None], loaded.Tack);
    }

    [Fact]
    public void CopyingADesignKeepsOnlyTheRequestedLayers()
    {
        var full = new StatueAppearance { SkinShapes = ["a"] };
        var trimmed = full.WithLayers(look: false, clothing: true, armor: false);

        Assert.Empty(trimmed.SkinShapes);
        Assert.False(trimmed.ShowLook);
        Assert.True(trimmed.ShowClothing);
        Assert.Equal(["a"], full.SkinShapes);
    }

    [Theory]
    [InlineData("hoovedwearables-head-bridle2", StatueTack.Piece.Bridle, "bridle2")]
    [InlineData("hoovedwearables-face-face12", StatueTack.Piece.Face, "face12")]
    [InlineData("hoovedwearables-middleback-saddle1", StatueTack.Piece.Saddle, "saddle1")]
    [InlineData("hoovedwearables-lowerbackside-saddlebags2", StatueTack.Piece.Bags, "saddlebags2")]
    public void WornElkTackIsRecognisedFromItsItemCode(string code, StatueTack.Piece piece, string type)
    {
        Assert.Equal((piece, type), StatueTack.FromWearableCode(code));
    }

    [Theory]
    [InlineData("hoovedwearables-head-bridle9")]
    [InlineData("hoovedwearables-neck-scarf1")]
    [InlineData("game-gear-temporal")]
    [InlineData("hoovedwearables-face")]
    public void OtherItemsAreNotElkTack(string code)
    {
        Assert.Null(StatueTack.FromWearableCode(code));
    }

    [Theory]
    [InlineData("antler-elk-01", 1)]
    [InlineData("antler-elk-08", 8)]
    [InlineData("antler-elk-09", 0)]
    [InlineData("antler-elk-xx", 0)]
    [InlineData("gear-temporal", 0)]
    [InlineData(null, 0)]
    public void AnElksAntlerSizeComesFromItsAntlerItem(string? code, int size)
    {
        Assert.Equal(size, StatueAppearance.AntlersFromItemCode(code));
    }

    [Fact]
    public void AntlersAreSavedAndAnImpossibleSizeIsRejectedOrClamped()
    {
        var tree = new TreeAttribute();
        new StatueAppearance { Antlers = 0 }.Write(tree);
        Assert.Equal(0, StatueAppearance.Read(tree, null!).Antlers);

        tree.SetInt("antlers", 99);
        Assert.Equal(StatueAppearance.MaxAntlers, StatueAppearance.Read(tree, null!).Antlers);

        var request = new EditRequest("A", "wave", 0, true, true, true, -1, -1, 0, StatueAppearance.ElkBull, StatuePoses.ElkStand, 0,
            StatueTack.Empty(), EditRequest.NoCopy, 9);
        Assert.False(request.HasValidMount(false));
        Assert.True((request with { Antlers = 0 }).HasValidMount(false));
    }

    [Fact]
    public void ASpecialStatueIsAStatueTypeNotABlockFamilyAndTheHandbookLeavesItOut()
    {
        var lang = Json("lang/en.json");
        Assert.DoesNotContain("robot", (string)lang["nf3tstatue:guide-text"]!, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Mod("blocktypes/robot.json")));
        Assert.False(File.Exists(Mod("blocktypes/robotmetal.json")));
        foreach (var type in StatueTypes.All) Assert.NotNull(lang[StatueTypes.LangKey(type)]);
        Assert.True(StatueTypes.IsKnown("robot"));
        Assert.False(StatueTypes.IsKnown("../etc"));
    }

    [Fact]
    public void ASpecialFigureFillsItsBlockAndEveryShapeExists()
    {
        foreach (var special in StatueTypes.Specials)
        {
            var path = special.ShapePath["nf3tstatue:".Length..];
            var shape = JObject.Parse(File.ReadAllText(Path.Combine(Root, "StatueMod/assets/nf3tstatue", path)));
            Assert.Contains(shape["elements"]!, e => (string?)e["name"] == StatueTypes.PedestalElement);

            // Scaled, the figure stands under a block's height (a small statue) and inside the 3x3x4 volume (a large one).
            Assert.InRange(special.FigureHeight * StatueTypes.ScaleFor(special, "small"), 0.8, 0.9);
            Assert.InRange(special.FigureHeight * StatueTypes.ScaleFor(special, "large"), 3.5, 3.8);
            Assert.Equal(StatueTypes.ScaleFor(special, "small"), StatueTypes.ScaleFor(special, "bust"));
        }
    }

    [Fact]
    public void ASpecialTypeIsSavedWithTheStatueAndSurvivesCopying()
    {
        var tree = new TreeAttribute();
        new StatueAppearance { Type = "robot" }.Write(tree);
        Assert.Equal("robot", StatueAppearance.Read(tree, null!).Type);
        Assert.Equal("robot", new StatueAppearance { Type = "robot" }.WithLayers(true, true, true).Type);

        tree.SetString("statueType", "nonsense");
        Assert.Equal(StatueTypes.Player, StatueAppearance.Read(tree, null!).Type);
        Assert.Equal(StatueTypes.Player, StatueAppearance.Read(new TreeAttribute(), null!).Type);
    }

    [Theory]
    [InlineData("statueblock")]
    [InlineData("statue")]
    [InlineData("statuemetal")]
    public void ALargeStatueOccupiesThreeByThreeBlocksAroundItsCentreAndOnlyThatSize(string file)
    {
        var type = Json($"blocktypes/{file}.json");
        var properties = type["behaviorsByType"]![$"{file}-large-*"]![0]!["properties"]!;

        Assert.Equal((3, 4, 3), ((int)properties["sizex"]!, (int)properties["sizey"]!, (int)properties["sizez"]!));
        Assert.Equal((1, 0, 1), ((int)properties["cposition"]!["x"]!, (int)properties["cposition"]!["y"]!, (int)properties["cposition"]!["z"]!));
        Assert.True((bool)properties["offsetHitboxes"]!);
        Assert.Empty(type["behaviorsByType"]!["*"]!);
    }

    [Fact]
    public void TheLargeBlankAndPlinthCoverTheThreeByThreeFootprint()
    {
        foreach (var (file, top) in new[] { ("blank-large", 56.0), ("pedestal-large", 3.0) })
        {
            var elements = Json($"shapes/block/{file}.json")["elements"]!;
            Assert.Equal(top, elements.Max(e => (double)e["to"]![1]!));
            // The centre block is the controller, so the shape reaches one block (16 pixels) out on every side of it.
            Assert.True(elements.Min(e => (double)e["from"]![0]!) <= -14 && elements.Max(e => (double)e["to"]![0]!) >= 30);
            Assert.True(elements.Min(e => (double)e["from"]![2]!) <= -14 && elements.Max(e => (double)e["to"]![2]!) >= 30);
        }
    }

    [Fact]
    public void ThePacketCarriesTheStatueType()
    {
        var request = new EditRequest("A", "wave", 0, true, true, true, -1, -1, 0, "", StatuePoses.ElkStand, 0, StatueTack.Empty(),
            EditRequest.NoCopy, 4, "robot");
        Assert.Equal("robot", EditRequest.Decode(request.Encode())!.StatueType);
    }

    [Fact]
    public void RemovingThePedestalIsOffByDefaultAndIsSavedAndSent()
    {
        Assert.False(new StatueAppearance().NoPedestal);
        Assert.False(StatueAppearance.Read(new TreeAttribute(), null!).NoPedestal);

        var tree = new TreeAttribute();
        new StatueAppearance { NoPedestal = true }.Write(tree);
        Assert.True(StatueAppearance.Read(tree, null!).NoPedestal);

        var request = new EditRequest("A", "wave", 0, true, true, true, -1, -1, 0, "", StatuePoses.ElkStand, 0, StatueTack.Empty(),
            EditRequest.NoCopy, 4, StatueTypes.Player, true);
        Assert.True(EditRequest.Decode(request.Encode())!.NoPedestal);
        Assert.True(new StatueAppearance { NoPedestal = true }.WithLayers(true, true, true).NoPedestal);
    }

    [Theory]
    [InlineData("small", 14, 5, 11)]
    [InlineData("bust", 14, 5, 11)]
    [InlineData("large", 54, 4, 12)]
    public void AMoldIsABlockWithAHoleInTheTopWhoseMetalRisesInTheHole(string size, int fillHeight, int from, int to)
    {
        var mold = Json("blocktypes/statuemold.json");
        var attributes = mold["attributesByType"]![$"statuemold-{size}-*"]!;
        Assert.Equal(fillHeight, (int)attributes["fillHeight"]!);
        // One quad for every pixel the metal can rise, level with the hole.
        var quads = attributes["fillQuadsByLevel"]!;
        Assert.Equal(fillHeight + 1, quads.Count());
        Assert.All(quads, q => Assert.Equal((from, to), ((int)q["x1"]!, (int)q["x2"]!)));

        var shape = Json($"shapes/block/mold-{(size == "large" ? "large" : "small")}.json")["elements"]!;
        // Nothing is in the way of the hole: no part of the clay covers its square above the floor.
        foreach (var e in shape.Where(e => (double)e["to"]![1]! > 1.5))
        {
            var x1 = (double)e["from"]![0]!; var x2 = (double)e["to"]![0]!;
            var z1 = (double)e["from"]![2]!; var z2 = (double)e["to"]![2]!;
            var overlapsHole = x1 < to && x2 > from && z1 < to && z2 > from;
            Assert.False(overlapsHole, $"{e["name"]} blocks the hole");
        }

        Assert.Equal(size == "large", mold["behaviorsByType"]![size == "large" ? "statuemold-large-*" : "*"]!.Any(b => (string?)b["name"] == "Multiblock"));
    }
}
