using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Nf3t.VintageStory.TapestryRepair.Tests;

/// <summary>The temporal loom is an item type, a shape, a recipe and lang entries that must agree with each other and the game.</summary>
public class TemporalLoomAssetsTests
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "../../../../");
    private static string ModDir => Path.Combine(Root, "TapestryRepairMod");
    private static string Mod(string path) => Path.Combine(ModDir, "assets/nf3ttapestryrepair", path);
    private static string Game(string path) => Path.Combine(Root, "vintagestory/assets/survival", path);

    private static JObject Json(string file) => JObject.Parse(File.ReadAllText(file));
    private static JObject ItemType() => Json(Mod("itemtypes/temporalsewingkit.json"));
    private static JObject BlockType() => Json(Mod("blocktypes/temporalloom.json"));
    private static JObject Shape() => Json(Mod("shapes/item/temporalsewingkit.json"));
    private static JObject LoomShape() => Json(Mod("shapes/block/temporalloom.json"));
    private static JObject FiberShape() => Json(Mod("shapes/item/temporalfiber.json"));
    private static JObject FiberType() => Json(Mod("itemtypes/temporalfiber.json"));
    private static JObject KitRecipe() => (JObject)JArray.Parse(File.ReadAllText(Mod("recipes/grid/temporalsewingkit.json")))[0];
    private static JObject Recipe() => (JObject)JArray.Parse(File.ReadAllText(Mod("recipes/grid/temporalloom.json")))[0];
    private static JObject Lang() => Json(Mod("lang/en.json"));

    private static string ModSystem => File.ReadAllText(Path.Combine(ModDir, "TapestryRepairModSystem.cs"));

    [Fact]
    public void TheKitItemCodeMatchesTheConstantAndItsClassIsRegistered()
    {
        Assert.Equal(ItemTemporalSewingKit.KitCode, "nf3ttapestryrepair:" + (string)ItemType()["code"]!);
        Assert.Contains($"RegisterItemClass(\"{(string)ItemType()["class"]!}\", typeof({nameof(ItemTemporalSewingKit)}))", ModSystem);
        Assert.True((int)ItemType()["durability"]! > 0, "the kit must be a durable tool");
    }

    [Fact]
    public void TheLoomIsAOneByTwoMultiblockWithItsClassRegisteredAndAShapePerSide()
    {
        var block = BlockType();
        Assert.Contains($"RegisterBlockClass(\"{(string)block["class"]!}\", typeof({nameof(BlockTemporalLoom)}))", ModSystem);
        var multiblock = block["behaviors"]!.Single(b => (string)b["name"]! == "Multiblock")["properties"]!;
        Assert.Equal(1, (int)multiblock["sizex"]!);
        Assert.Equal(2, (int)multiblock["sizey"]!);
        Assert.Equal(1, (int)multiblock["sizez"]!);
        Assert.Contains(block["behaviors"]!, b => (string)b["name"]! == "HorizontalOrientable");
        foreach (var side in new[] { "north", "east", "south", "west" })
            Assert.Equal("nf3ttapestryrepair:block/temporalloom", (string)block["shapebytype"]![$"*-{side}"]!["base"]!);
        Assert.Equal(1.5, (double)block["collisionbox"]!["y2"]!);
        Assert.Equal(1.5, (double)block["selectionbox"]!["y2"]!);
    }

    [Fact]
    public void TheLoomHasABlockEntityAndTheFiberItemExists()
    {
        var entity = (string)BlockType()["entityClass"]!;
        Assert.Contains($"RegisterBlockEntityClass(\"{entity}\", typeof({nameof(BlockEntityTemporalLoom)}))", ModSystem);
        Assert.Equal(BlockEntityTemporalLoom.FiberCode, "nf3ttapestryrepair:" + (string)FiberType()["code"]!);
        Assert.Equal("nf3ttapestryrepair:item/temporalfiber", (string)FiberType()["shape"]!["base"]!);
        var light = FiberType()["lightHsv"]!.Select(v => (int)v).ToArray();
        Assert.InRange(light[0], 28, 36); // blue-green, like the temporal gear
        Assert.True(light[2] > 0, "the fiber must emit light");
    }

    [Fact]
    public void TheLoomFiberAndKitHaveHandbookSectionsWithExistingLangKeys()
    {
        var lang = Lang();
        var types = new[] { BlockType()["attributes"]!, FiberType()["attributes"]!, ItemType()["attributes"]! };
        foreach (var attributes in types)
        foreach (var section in attributes["handbook"]!["extraSections"]!)
        foreach (var field in new[] { "title", "text" })
        {
            var key = (string)section[field]!;
            Assert.StartsWith("nf3ttapestryrepair:", key);
            Assert.True(lang[key["nf3ttapestryrepair:".Length..]] != null, $"handbook {field} {key} is not in en.json");
        }
        Assert.Contains("temporalloom-*", BlockType()["attributes"]!["handbook"]!["groupBy"]!.Select(g => (string)g!));
    }

    [Fact]
    public void TheDecayKitIsMadeFromDecayedFiberAndAStick()
    {
        var item = Json(Mod("itemtypes/temporaldecaykit.json"));
        Assert.Equal(ItemTemporalDecayKit.KitCode, "nf3ttapestryrepair:" + (string)item["code"]!);
        Assert.Contains($"RegisterItemClass(\"{(string)item["class"]!}\", typeof({nameof(ItemTemporalDecayKit)}))", ModSystem);
        Assert.Equal(3, (int)item["durability"]!);
        Assert.True(File.Exists(Mod("shapes/item/temporaldecaykit.json")));
        var recipe = (JObject)JArray.Parse(File.ReadAllText(Mod("recipes/grid/temporaldecaykit.json")))[0];
        Assert.Equal(ItemTemporalDecayKit.KitCode, (string)recipe["output"]!["code"]!);
        Assert.Equal(BlockEntityDecayLoom.DecayedFiberCode, (string)recipe["ingredients"]!["F"]!["code"]!);
        Assert.Equal("game:stick", (string)recipe["ingredients"]!["S"]!["code"]!);
    }

    [Fact]
    public void TheUnravelingLoomMirrorsTheLoomOnRustyGearsAndMakesDecayedFiber()
    {
        var block = Json(Mod("blocktypes/decayloom.json"));
        Assert.Contains($"RegisterBlockClass(\"{(string)block["class"]!}\", typeof({nameof(BlockDecayLoom)}))", ModSystem);
        Assert.Contains($"RegisterBlockEntityClass(\"{(string)block["entityClass"]!}\", typeof({nameof(BlockEntityDecayLoom)}))", ModSystem);
        var multiblock = block["behaviors"]!.Single(x => (string)x["name"]! == "Multiblock")["properties"]!;
        Assert.Equal(2, (int)multiblock["sizey"]!);
        foreach (var side in new[] { "north", "east", "south", "west" })
        {
            Assert.Equal("nf3ttapestryrepair:block/decayloom", (string)block["shapebytype"]![$"*-{side}"]!["base"]!);
            Assert.NotNull(Lang()[$"block-decayloom-{side}"]);
        }
        Assert.True(File.Exists(Mod("shapes/block/decayloom.json")));
        var recipe = (JObject)JArray.Parse(File.ReadAllText(Mod("recipes/grid/decayloom.json")))[0];
        Assert.Equal("nf3ttapestryrepair:decayloom-north", (string)recipe["output"]!["code"]!);
        Assert.Equal(BlockDecayLoom.RustyGearCode, (string)recipe["ingredients"]!["G"]!["code"]!);
        var fiber = Json(Mod("itemtypes/decayedfiber.json"));
        Assert.Equal(BlockEntityDecayLoom.DecayedFiberCode, "nf3ttapestryrepair:" + (string)fiber["code"]!);
        Assert.Contains("gear-rusty", File.ReadAllText(Game("itemtypes/resource/gear.json")));
        Assert.True(File.Exists(Mod("textures/item/decayedfiber.png")));
        foreach (var shape in new[] { "block/decayloom", "item/decayedfiber", "item/decayedfiber-pile" })
            Assert.True(File.Exists(Mod($"shapes/{shape}.json")));
    }

    [Fact]
    public void TheKitRecipeUsesFiberAndMakesTheKit()
    {
        var recipe = KitRecipe();
        Assert.Equal(ItemTemporalSewingKit.KitCode, (string)recipe["output"]!["code"]!);
        Assert.Equal(BlockEntityTemporalLoom.FiberCode, (string)recipe["ingredients"]!["F"]!["code"]!);
        Assert.Equal("game:stick", (string)recipe["ingredients"]!["S"]!["code"]!);
        Assert.Equal(3, (int)ItemType()["durability"]!);
        var pattern = ((string)recipe["ingredientPattern"]!).Replace(",", "");
        Assert.Equal((int)recipe["width"]! * (int)recipe["height"]!, pattern.Length);
        Assert.Equal(((JObject)recipe["ingredients"]!).Properties().Select(p => p.Name).ToHashSet(), pattern.Select(c => c.ToString()).ToHashSet());
    }

    [Fact]
    public void TheCodeCountsAsPowerAndFlaxMatchVanillaItems()
    {
        Assert.Equal("game:gear-temporal", BlockTemporalLoom.GearCode);
        Assert.Equal("game:flaxtwine", BlockTemporalLoom.FlaxCode);
        Assert.True(File.Exists(Game("itemtypes/resource/flaxtwine.json")));
        Assert.Contains("gear-temporal", File.ReadAllText(Game("itemtypes/resource/gear.json")));
    }

    [Fact]
    public void TheLoomShapeFitsItsOneAndAHalfBlocks()
    {
        var top = LoomShape()["elements"]!.Max(e => (double)e["to"]![1]!);
        Assert.InRange(top, 20, 24);
    }

    [Fact]
    public void TheItemTypePointsAtTheShape()
    {
        Assert.Equal("nf3ttapestryrepair:item/temporalsewingkit", (string)ItemType()["shape"]!["base"]!);
        Assert.True(File.Exists(Mod("shapes/item/temporalsewingkit.json")));
        Assert.True(File.Exists(Mod("shapes/block/temporalloom.json")));
    }

    [Fact]
    public void EveryShapeTextureExists()
    {
        foreach (var shape in new[] { Shape(), LoomShape(), FiberShape(), Json(Mod("shapes/item/temporaldecaykit.json")), Json(Mod("shapes/block/decayloom.json")), Json(Mod("shapes/item/decayedfiber.json")), Json(Mod("shapes/item/decayedfiber-pile.json")) })
        foreach (var (key, value) in (JObject)shape["textures"]!)
        {
            var path = (string)value!;
            var file = path.StartsWith("game:")
                ? Game("textures/" + path["game:".Length..] + ".png")
                : Mod("textures/" + path["nf3ttapestryrepair:".Length..] + ".png");
            Assert.True(path.StartsWith("game:") || path.StartsWith("nf3ttapestryrepair:"), $"texture {key} = {path} is from neither the game nor this mod");
            Assert.True(File.Exists(file), $"texture {key} = {path} does not exist");
        }
    }

    [Fact]
    public void EveryFaceUsesADeclaredTextureAndAUvInsideIt()
    {
        foreach (var shape in new[] { Shape(), LoomShape(), FiberShape(), Json(Mod("shapes/item/temporaldecaykit.json")), Json(Mod("shapes/block/decayloom.json")), Json(Mod("shapes/item/decayedfiber.json")), Json(Mod("shapes/item/decayedfiber-pile.json")) }) CheckFaces(shape);
    }

    private static void CheckFaces(JObject shape)
    {
        var textures = ((JObject)shape["textures"]!).Properties().Select(p => "#" + p.Name).ToHashSet();
        var elements = shape["elements"]!.ToArray();
        Assert.NotEmpty(elements);
        foreach (var element in elements)
        foreach (var face in ((JObject)element["faces"]!).Properties().Where(f => (bool?)f.Value["enabled"] != false))
        {
            var where = $"{element["name"]} {face.Name}";
            Assert.Contains((string)face.Value["texture"]!, textures);
            var uv = face.Value["uv"]!.Select(v => (double)v).ToArray();
            Assert.Equal(4, uv.Length);
            Assert.True(uv.All(v => v is >= 0 and <= 16), $"{where} uv {string.Join(",", uv)} is outside the 16x16 texture");
            Assert.True(uv[2] > uv[0] && uv[3] > uv[1], $"{where} uv {string.Join(",", uv)} is empty or flipped");
        }
    }

    [Fact]
    public void TheRecipeMakesTheLoomBlockAndItsPatternUsesEveryIngredient()
    {
        var recipe = Recipe();
        Assert.Equal("block", (string)recipe["output"]!["type"]!);
        Assert.Equal("nf3ttapestryrepair:" + (string)BlockType()["code"] + "-north", (string)recipe["output"]!["code"]!);
        var pattern = ((string)recipe["ingredientPattern"]!).Replace(",", "");
        Assert.Equal((int)recipe["width"]! * (int)recipe["height"]!, pattern.Length);
        var keys = ((JObject)recipe["ingredients"]!).Properties().Select(p => p.Name).ToHashSet();
        Assert.Equal(keys, pattern.Where(c => c != '_' && c != ' ').Select(c => c.ToString()).ToHashSet());
    }

    /// <summary>
    /// A "game:base-state" ingredient exists when some vanilla item/block type has code "base" and mentions "state" as a
    /// variant state, either in its own file or in the world-properties file it loads its states from.
    /// </summary>
    [Fact]
    public void EveryRecipeIngredientExistsInTheGame()
    {
        foreach (var (key, ingredient) in (JObject)Recipe()["ingredients"]!)
        {
            var type = (string)ingredient!["type"]!;
            var code = (string)ingredient["code"]!;
            if (!code.StartsWith("game:")) continue; // this mod's own items are covered by their own tests
            var parts = code["game:".Length..].Split('-');
            var dir = Game(type == "block" ? "blocktypes" : "itemtypes");
            var codePattern = new Regex($"""\bcode\W+{Regex.Escape(parts[0])}["']""");
            // Several files can share a base code (e.g. "metal"); one of them must have every state.
            var candidates = Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories)
                .Select(File.ReadAllText).Where(t => codePattern.IsMatch(t)).ToList();
            Assert.True(candidates.Count > 0, $"{key}: no vanilla {type} type with code {parts[0]}");

            bool HasStates(string text)
            {
                foreach (Match m in Regex.Matches(text, """loadFromProperties\W+([\w/]+)"""))
                    text += File.ReadAllText(Game("worldproperties/" + m.Groups[1].Value + ".json"));
                return parts.Skip(1).All(state => text.Contains($"\"{state}\""));
            }

            Assert.True(candidates.Any(HasStates), $"{key}: no vanilla {type} type {parts[0]} has the states of {code}");
        }
    }

    [Fact]
    public void EveryLangKeyTheModUsesExists()
    {
        var lang = Lang();
        Assert.NotNull(lang["item-temporalfiber"]);
        Assert.NotNull(lang["itemdesc-temporalfiber"]);
        Assert.NotNull(lang["item-temporalsewingkit"]);
        Assert.NotNull(lang["itemdesc-temporalsewingkit"]);
        foreach (var side in new[] { "north", "east", "south", "west" }) Assert.NotNull(lang[$"block-temporalloom-{side}"]);
        foreach (var source in Directory.EnumerateFiles(ModDir, "*.cs"))
        foreach (Match m in Regex.Matches(File.ReadAllText(source), "\"nf3ttapestryrepair:([\\w-]+)\""))
        {
            var key = m.Groups[1].Value;
            if (key is "temporalsewingkit" or "temporalfiber" or "temporaldecaykit" or "decayedfiber") continue; // item codes, not lang keys
            Assert.True(lang[key] != null, $"{Path.GetFileName(source)} uses lang key {key}, which en.json lacks");
        }
    }

    [Fact]
    public void TheSoundsTheModPlaysExistInTheGame()
    {
        var source = string.Concat(Directory.EnumerateFiles(ModDir, "*.cs").Select(File.ReadAllText));
        var sounds = Regex.Matches(source, "\"(sounds/[\\w/-]+)\"").Select(m => m.Groups[1].Value).ToList();
        Assert.NotEmpty(sounds);
        foreach (var sound in sounds)
            Assert.True(File.Exists(Game(sound + ".ogg")), $"{sound} is not in the game");
    }
}
