#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Nf3t.VintageStory.Screenshots;

/// <summary>Records where, in which world and on which server each screenshot was taken, and lists, labels, opens
/// and navigates to them with <c>.shots</c>.</summary>
public class ScreenshotsModSystem : ModSystem
{
    public const string ConfigFile = "nf3tscreenshots.json";

    /// <summary>Event bus message handled by Translocator Navigator (2.2.0+): a tree with absolute <c>x</c>, <c>y</c>,
    /// <c>z</c> ints and a <c>label</c> string; it plans a route there and starts guidance.</summary>
    public const string NavigateEvent = "nf3ttranslocatornavigator:navigate";

    public const string NavigatorModId = "nf3ttranslocatornavigator";

    private const int PageSize = 8;

    private ICoreClientAPI? api;
    private Harmony? harmony;
    private ScreenshotsConfig config = new();
    private ScreenshotIndex index = new();
    private string indexPath = "";
    private ScreenshotRecord? pending;

    public override bool ShouldLoad(EnumAppSide side)
    {
        return side == EnumAppSide.Client;
    }

    public override void StartClientSide(ICoreClientAPI capi)
    {
        api = capi;
        LoadConfig(capi);
        indexPath = Path.Combine(GamePaths.DataPath, "ModData", "Nf3tScreenshots.json");
        try
        {
            index = ScreenshotIndex.Load(indexPath);
        }
        catch (Exception e)
        {
            // Keep the unreadable file for the player to look at instead of overwriting it with an empty index.
            capi.Logger.Error("[Screenshots] Could not read {0}, recording to a new file: {1}", indexPath, e.Message);
            indexPath = Path.Combine(GamePaths.DataPath, "ModData", $"Nf3tScreenshots-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        }

        ScreenshotPatches.Mod = this;
        harmony = new Harmony(ScreenshotPatches.HarmonyId);
        if (!ScreenshotPatches.Apply(harmony, message => capi.Logger.Error(message)))
            harmony = null;

        CreateCommands(capi);
    }

    public override void Dispose()
    {
        harmony?.UnpatchAll(ScreenshotPatches.HarmonyId);
        harmony = null;
        if (ScreenshotPatches.Mod == this) ScreenshotPatches.Mod = null;
    }

    private void LoadConfig(ICoreClientAPI capi)
    {
        try
        {
            config = capi.LoadModConfig<ScreenshotsConfig>(ConfigFile) ?? new ScreenshotsConfig();
        }
        catch (Exception e)
        {
            capi.Logger.Error("[Screenshots] Could not read ModConfig/{0}, using defaults: {1}", ConfigFile, e.Message);
            config = new ScreenshotsConfig();
        }

        capi.StoreModConfig(config, ConfigFile);
    }

    internal void LogError(Exception e)
    {
        api?.Logger.Error("[Screenshots] {0}", e);
    }

    /// <summary>Called just before the game saves an image. Returns EXIF text to use instead of the game's, or null to
    /// leave the game's alone.</summary>
    internal string? OnBeforeSave(bool mega)
    {
        if (api?.World?.Player?.Entity == null) return null;
        pending = ScreenshotCapture.Capture(api);
        pending.Mega = mega;
        return config.WriteExif && !pending.CoordinatesHidden ? ScreenshotCapture.ExifJson(pending) : null;
    }

    /// <summary>Called after the game saved an image to <paramref name="path"/> (null: the screenshots folder).</summary>
    internal void OnSaved(string? path, string? filename, string? savedName)
    {
        var record = pending;
        pending = null;
        if (api == null || record == null || string.IsNullOrEmpty(savedName)) return;
        // Something other than the screenshot system chose the file name; not one of the player's screenshots.
        if (filename != null) return;

        if (path == null)
        {
            record.File = savedName!;
        }
        else
        {
            if (!config.IndexTimelapse) return;
            var relative = Path.GetRelativePath(GamePaths.Screenshots, Path.Combine(path, savedName!));
            if (relative.StartsWith("..", StringComparison.Ordinal)) return;
            record.File = relative.Replace('\\', '/');
        }

        index.Add(record);
        try
        {
            index.Save(indexPath);
        }
        catch (Exception e)
        {
            api.Logger.Error("[Screenshots] Could not save {0}: {1}", indexPath, e.Message);
        }
    }

    private string WorldId => api!.World.SavegameIdentifier;
    private bool HidesCoordinates => ScreenshotCapture.WorldHidesCoordinates(api!);

    private void CreateCommands(ICoreClientAPI capi)
    {
        var parsers = capi.ChatCommands.Parsers;
        capi.ChatCommands.Create("shots")
            .WithDescription("Lists the screenshots taken in this world, newest first (8 per page).")
            .WithArgs(parsers.OptionalInt("page", 1))
            .HandleWith(args => List((int)args[0]))
            .BeginSubCommand("near")
            .WithDescription("Lists this world's screenshots taken within a radius (default 500 blocks), nearest first.")
            .WithArgs(parsers.OptionalInt("radius", 500))
            .HandleWith(args => Near((int)args[0]))
            .EndSubCommand()
            .BeginSubCommand("info")
            .WithDescription("Shows everything recorded about a screenshot (number from .shots, or file name).")
            .WithArgs(parsers.Word("screenshot"))
            .HandleWith(args => Info((string)args[0]))
            .EndSubCommand()
            .BeginSubCommand("goto")
            .WithDescription("Plans a route to where a screenshot was taken with Translocator Navigator.")
            .WithArgs(parsers.Word("screenshot"))
            .HandleWith(args => Goto((string)args[0]))
            .EndSubCommand()
            .BeginSubCommand("open")
            .WithDescription("Opens a screenshot in your image viewer.")
            .WithArgs(parsers.Word("screenshot"))
            .HandleWith(args => Open((string)args[0]))
            .EndSubCommand()
            .BeginSubCommand("label")
            .WithDescription("Names a screenshot, e.g. .shots label 3 Lake house. Leave the text empty to clear it.")
            .WithArgs(parsers.Word("screenshot"), parsers.OptionalAll("text"))
            .HandleWith(args => Label((string)args[0], args[1] as string))
            .EndSubCommand()
            .BeginSubCommand("refresh")
            .WithDescription("Imports screenshots taken before this mod was installed, from the location data the game wrote into them. Screenshots without that data are ignored.")
            .HandleWith(_ => Refresh())
            .EndSubCommand()
            .BeginSubCommand("worlds")
            .WithDescription("Counts the recorded screenshots per world and server.")
            .HandleWith(_ => Worlds())
            .EndSubCommand();
    }

    private TextCommandResult List(int page)
    {
        var shots = index.ForWorld(WorldId);
        if (shots.Count == 0)
            return TextCommandResult.Success("No screenshots recorded in this world yet. Press F12 to take one.");

        var pages = (shots.Count + PageSize - 1) / PageSize;
        page = Math.Clamp(page, 1, pages);
        var player = ScreenshotCapture.PlayerPos(api!);
        var hidden = HidesCoordinates;
        var text = new StringBuilder($"<strong>Screenshots in this world</strong> ({shots.Count}, page {page} of {pages}):");
        for (var i = (page - 1) * PageSize; i < Math.Min(shots.Count, page * PageSize); i++)
            text.Append('\n').Append(ScreenshotText.Describe(shots[i], i + 1, player.X, player.Z, hidden));
        if (hidden) text.Append("\nThis world hides coordinates, so positions are not shown.");
        return TextCommandResult.Success(text.ToString());
    }

    private TextCommandResult Near(int radius)
    {
        if (HidesCoordinates)
            return TextCommandResult.Error("This world hides coordinates, so screenshots can't be searched by distance.");
        var player = ScreenshotCapture.PlayerPos(api!);
        var shots = index.Near(WorldId, player.X, player.Z, Math.Max(1, radius));
        if (shots.Count == 0) return TextCommandResult.Success($"No screenshots within {radius} blocks.");
        var text = new StringBuilder($"<strong>Screenshots within {radius} blocks</strong> ({shots.Count}):");
        foreach (var shot in shots.Take(PageSize * 2))
            text.Append('\n').Append(ScreenshotText.Describe(shot, index.NumberOf(shot), player.X, player.Z, false));
        return TextCommandResult.Success(text.ToString());
    }

    private TextCommandResult Info(string key)
    {
        var shot = index.Find(WorldId, key);
        if (shot == null) return NotFound(key);
        var player = ScreenshotCapture.PlayerPos(api!);
        var text = new StringBuilder(ScreenshotText.Describe(shot, index.NumberOf(shot), player.X, player.Z,
            HidesCoordinates));
        text.Append($"\nTaken {shot.TakenUtc.ToLocalTime():yyyy-MM-dd HH:mm}");
        if (shot.Mega) text.Append(" (mega screenshot)");
        var climate = ScreenshotText.Climate(shot);
        if (climate.Length > 0) text.Append($"\n{climate}");
        var server = string.Join(", ", new[] { shot.ServerName, shot.ServerAddress }.Where(s => !string.IsNullOrWhiteSpace(s)));
        text.Append(server.Length > 0 ? $"\nServer: {server}" : "\nSingleplayer");
        if (shot.Source == ScreenshotImport.VanillaSource)
            text.Append("\nImported from the game's own location data (matched by seed).");
        if (!File.Exists(FullPath(shot))) text.Append("\nThe image file has been moved or deleted.");
        return TextCommandResult.Success(text.ToString());
    }

    private TextCommandResult Goto(string key)
    {
        var shot = index.Find(WorldId, key);
        if (shot == null) return NotFound(key);
        if (ScreenshotText.IsHidden(shot, HidesCoordinates))
            return TextCommandResult.Error("This world hides coordinates, so the mod won't lead you to a screenshot.");

        var name = string.IsNullOrWhiteSpace(shot.Label) ? shot.File : shot.Label!;
        var navigatorVersion = api!.ModLoader.IsModEnabled(NavigatorModId)
            ? api.ModLoader.GetMod(NavigatorModId)?.Info?.Version
            : null;
        if (NavigatorCompatibility.SupportsRouteEvent(navigatorVersion))
        {
            var data = new TreeAttribute();
            data.SetInt("x", (int)Math.Floor(shot.X));
            data.SetInt("y", (int)Math.Floor(shot.Y));
            data.SetInt("z", (int)Math.Floor(shot.Z));
            data.SetString("label", name);
            api.Event.PushEvent(NavigateEvent, data);
            return TextCommandResult.Success($"Planning a route to {name}.");
        }

        var player = ScreenshotCapture.PlayerPos(api);
        var needs = navigatorVersion == null
            ? "Install Translocator Navigator to get a route there, through your known translocators."
            : $"Translocator Navigator {navigatorVersion} is installed, but routes need 2.2.0 or later.";
        return TextCommandResult.Success(
            $"{name} was taken at {shot.RelX}, {shot.RelY}, {shot.RelZ} " +
            $"({(int)shot.HorizontalDistanceTo(player.X, player.Z)} m " +
            $"{ScreenshotText.Compass(player.X, player.Z, shot.X, shot.Z)}). {needs}");
    }

    private TextCommandResult Open(string key)
    {
        var shot = index.Find(WorldId, key);
        if (shot == null) return NotFound(key);
        var path = FullPath(shot);
        if (!File.Exists(path)) return TextCommandResult.Error($"{shot.File} has been moved or deleted.");
        NetUtil.OpenUrlInBrowser(path);
        return TextCommandResult.Success($"Opening {shot.File}.");
    }

    private TextCommandResult Label(string key, string? text)
    {
        var shot = index.Find(WorldId, key);
        if (shot == null) return NotFound(key);
        shot.Label = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        try
        {
            index.Save(indexPath);
        }
        catch (Exception e)
        {
            return TextCommandResult.Error($"Could not save the label: {e.Message}");
        }

        return TextCommandResult.Success(shot.Label == null
            ? $"Removed the label of {shot.File}."
            : $"{shot.File} is now \"{shot.Label}\".");
    }

    private TextCommandResult Refresh()
    {
        var folder = GamePaths.Screenshots;
        if (!Directory.Exists(folder)) return TextCommandResult.Success("No screenshots folder yet.");

        var worldId = WorldId;
        var spawn = api!.World.DefaultSpawnPosition.AsBlockPos;
        int imported = 0, inThisWorld = 0, unknown = 0, ignored = 0, already = 0;
        foreach (var path in Directory.EnumerateFiles(folder, "*.png", SearchOption.TopDirectoryOnly))
        {
            var file = Path.GetFileName(path);
            if (index.Contains(file))
            {
                already++;
                continue;
            }

            string? description;
            try
            {
                description = ExifReader.ReadImageDescription(path);
            }
            catch (IOException)
            {
                ignored++;
                continue;
            }

            var record = ScreenshotImport.FromVanillaExif(file, File.GetLastWriteTimeUtc(path), description, worldId,
                api.World.Seed, spawn.X, spawn.Z);
            if (record == null)
            {
                ignored++;
                continue;
            }

            index.Add(record);
            imported++;
            if (record.SavegameId == worldId) inThisWorld++;
            else unknown++;
        }

        if (imported > 0)
        {
            try
            {
                index.Save(indexPath);
            }
            catch (Exception e)
            {
                return TextCommandResult.Error($"Could not save the imported screenshots: {e.Message}");
            }
        }

        return TextCommandResult.Success(
            $"Imported {imported} screenshot(s): {inThisWorld} in this world (same seed), {unknown} in an unknown world. " +
            $"Already recorded: {already}. Ignored (no game location data): {ignored}.");
    }

    private TextCommandResult Worlds()
    {
        var worlds = index.Worlds();
        if (worlds.Count == 0) return TextCommandResult.Success("No screenshots recorded yet.");
        var text = new StringBuilder("<strong>Recorded screenshots</strong>:");
        foreach (var (name, id, count) in worlds)
            text.Append($"\n{name}: {count}{(id == WorldId ? " (this world)" : "")}");
        return TextCommandResult.Success(text.ToString());
    }

    private static TextCommandResult NotFound(string key)
    {
        return TextCommandResult.Error($"No screenshot \"{key}\" in this world. Use .shots to see their numbers.");
    }

    private static string FullPath(ScreenshotRecord shot)
    {
        return Path.Combine(GamePaths.Screenshots, shot.File);
    }
}
