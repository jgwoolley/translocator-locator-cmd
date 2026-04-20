using System.Text;
using Vintagestory.API.Client;

namespace Nf3t.VintageStory.Common;

// ReSharper disable once InconsistentNaming
public class Nf3tConfig
{
    private const string ConfigName = "Nf3tConfig.json";

    public int DefaultSearchRadius { get; set; } = 150;
    public List<BlockSelector> Selectors { get; set; } = new();
    public List<LocatorCommand> Commands { get; set; } = new();
    public bool EnableTranslocatorPath { get; set; }

    public string GenerateCommandsTable()
    {
        var sb = new StringBuilder();

        sb.AppendLine("<table>");
        sb.AppendLine("  <tr><th>Command</th><th>Description</th></tr>");
        sb.AppendLine("  <tr><td>.findtl</td><td>Finds nearby translocators.</td></tr>");

        foreach (var cmd in Commands) sb.AppendLine($"  <tr><td>.{cmd.Name}</td><td>{cmd.Description}</td></tr>");

        sb.AppendLine("</table>");
        return sb.ToString();
    }

    private static Nf3tConfig GetDefault()
    {
        return new Nf3tConfig
        {
            EnableTranslocatorPath = false,
            DefaultSearchRadius = 150,
            Selectors = new List<BlockSelector>
            {
                new("gear-rusty", "gear", "brown", ["treasure"]),
                new("loosegears", "gear", "brown", ["treasure"]),
                new("tapestry", "vessel", "blue", ["art", "treasure"]),
                new("painting", "vessel", "blue", ["art", "treasure"]),
                new("chandelier", "vessel", "brown", ["art", "treasure"]),
                new("trunk", "vessel", "brown", ["chest", "treasure"]),
                new("lootvessel", "vessel", "brown", ["chest", "treasure"]),
                new("storagevessel", "vessel", "brown", ["chest", "treasure"]),
                new("talldisplaycase", "vessel", "brown", ["chest", "treasure"]),
                new("displaycase", "vessel", "brown", ["chest", "treasure"]),
                new("bonysoil", "rocks", "brown", ["soil"]),
                new("soil-high", "rocks", "brown", ["soil"]),
                new("soil-compost", "rocks", "brown", ["soil"]),
                new("rawclay-fire", "rocks", "orange", ["clay"]),
                new("rawclay-red", "rocks", "red", ["clay"]),
                new("rawclay-blue", "rocks", "blue", ["clay"]),
                new("rock-whitemarble", "rocks", "white", ["marble"]),
                new("rock-redmarble", "rocks", "red", ["marble"]),
                new("rock-greenmarble", "rocks", "green", ["marble"]),
                new("looseores", "pick", "yellow", ["ore"]),
                new("ore", "pick", "red", ["ore"]),
                new("crystal", "rocks", "black", ["ore"]),
                new("wildbeehives", "bee", "yellow", ["bees"]),
                new("skeep", "bee", "yellow", ["bees"]),
                new("log-resin", "tree", "yellow", ["resin"]),
                new("mushroom", "mushroom", "red", ["plant"]),
                new("fruittree", "tree2", "red", ["fruit"]),
                new("tallplant-tule", "x", "red", ["plant"]),
                new("tallplant-coopersreed", "x", "red", ["plant"]),
                new("tallplant-papyrus", "x", "red", ["plant"]),
                new("bigberrybush", "berries", "red", ["plant"]),
                new("smallberrybush", "berries", "red", ["plant"]),
                new("flower", "x", "red", ["plant"])
            },
            Commands = new List<LocatorCommand>
            {
                new("findtreasure", "Finds nearby treasure, and gears.", false, "treasure"),
                new("findart", "Finds nearby tapestries, paintings, and chandeliers.", false, "art"),
                new("findchest", "Finds nearby item containers.", false, "chest"),
                new("findsoil", "Finds nearby bony soil, and high fertility soil.", true, "soil"),
                new("findclay", "Finds nearby clay.", true, "clay"),
                new("findore", "Finds nearby ores, and crystals.", true, "ore"),
                new("findbees", "Finds nearby bees.", false, "bees"),
                new("findresin", "Finds nearby resin.", false, "resin"),
                new("findplants", "Finds nearby mushrooms, reeds, bushes, and flowers.", true, "plant"),
                new("findfruit", "Finds nearby fruit trees.", false, "fruit"),
                new("findmarble", "Finds nearby marble.", true, "marble")
            }
        };
    }

    public static Nf3tConfig Load(ICoreClientAPI api)
    {
        Nf3tConfig? config = null;

        try
        {
            config = api.LoadModConfig<Nf3tConfig>(ConfigName);
        }
        catch (Exception e)
        {
            api.Logger.Error("Failed to load mod config, using defaults. Error: " + e.Message);
        }

        if (config == null)
        {
            api.Logger.Notification("Config missing or invalid, generating default...");
            config = GetDefault();

            // Save the clean default so the user has a valid file to edit
            api.StoreModConfig(config, ConfigName);
        }

        return config;
    }
}