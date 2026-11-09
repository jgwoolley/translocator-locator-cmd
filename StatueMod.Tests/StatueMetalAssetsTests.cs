using Newtonsoft.Json.Linq;
using Xunit;

namespace Nf3t.VintageStory.Statue.Tests;

/// <summary>The metal statues are described by a block type, recipes and lang entries that must agree with each other and the game.</summary>
public class StatueMetalAssetsTests
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "../../../../");
    private static string Mod(string path) => Path.Combine(Root, "StatueMod/assets/nf3tstatue", path);

    private static string[] Metals() =>
        JObject.Parse(File.ReadAllText(Mod("blocktypes/statuemetal.json")))["variantgroups"]!
            .First(g => (string?)g["code"] == "metal")["states"]!.Select(s => (string)s!).ToArray();

    /// <summary>The texture the block type gives a metal: the first texturesByType entry that matches, as the game picks it.</summary>
    private static string TextureOf(string metal)
    {
        var byType = (JObject)JObject.Parse(File.ReadAllText(Mod("blocktypes/statuemetal.json")))["texturesByType"]!;
        foreach (var (pattern, textures) in byType)
        {
            var wildcard = pattern.Replace("*", ".*");
            if (System.Text.RegularExpressions.Regex.IsMatch($"statuemetal-small-{metal}", "^" + wildcard + "$"))
                return ((string)textures["stone"]!["base"]!).Replace("{metal}", metal);
        }

        throw new Xunit.Sdk.XunitException($"no texture rule for {metal}");
    }

    [Fact]
    public void EveryMetalResolvesToATextureThatExistsInTheGame()
    {
        foreach (var metal in Metals())
        {
            var texture = TextureOf(metal);
            Assert.True(texture.StartsWith("game:block/metal/sheet-plain/") || texture.StartsWith("game:block/metal/plate/"),
                $"{metal} uses {texture}, which is neither a plain sheet nor a plate texture");
            var file = Path.Combine(Root, "vintagestory/assets/survival/textures/" + texture["game:".Length..] + ".png");
            Assert.True(File.Exists(file), $"{metal} uses {texture}, which is not in the game");
        }
    }

    [Fact]
    public void EveryMetalHasADisplayName()
    {
        var lang = JObject.Parse(File.ReadAllText(Mod("lang/en.json")));
        foreach (var metal in Metals()) Assert.NotNull(lang[$"nf3tstatue:metal-{metal}"]);
    }

    [Theory]
    [InlineData("gold", true)]
    [InlineData("silver", true)]
    [InlineData("copper", true)]
    [InlineData("iron", false)]
    [InlineData("lead", false)]
    [InlineData(null, false)]
    public void MetalsShineExceptIronAndLeadAndRockDoesNot(string? metal, bool expected)
    {
        Assert.Equal(expected, StatueMaterial.IsShiny(metal));
    }

    [Fact]
    public void GoldUsesTheSmoothPlateTextureNotTheSpeckledSheet()
    {
        Assert.Equal("game:block/metal/plate/gold", TextureOf("gold"));
        // The other metals keep the plain sheet texture.
        Assert.Equal("game:block/metal/sheet-plain/silver1", TextureOf("silver"));
    }
}
