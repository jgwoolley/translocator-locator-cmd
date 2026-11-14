#nullable enable

using System;
using HarmonyLib;

namespace Nf3t.VintageStory.Screenshots;

/// <summary>
/// The game has no "screenshot taken" event, so two of its internal methods are patched. Both are only called by
/// the game's screenshot system (F12, mega screenshots and timelapse frames):
/// <list type="bullet">
/// <item><c>SystemScreenshot.GetMetaData</c> runs just before each image is saved: the player's position is read
/// here, and the EXIF text is replaced when <see cref="ScreenshotsConfig.WriteExif"/> is on.</item>
/// <item><c>ClientPlatformWindows.SaveScreenshot</c> (used on every OS) returns the saved file name.</item>
/// </list>
/// If either is missing in a future game version, the mod logs it and stays off instead of failing.
/// </summary>
public static class ScreenshotPatches
{
    public const string HarmonyId = "nf3tscreenshots";

    private const string MetaDataType = "Vintagestory.Client.NoObf.SystemScreenshot";
    private const string PlatformType = "Vintagestory.Client.NoObf.ClientPlatformWindows";

    internal static ScreenshotsModSystem? Mod;

    public static bool Apply(Harmony harmony, Action<string> logError)
    {
        var getMetaData = AccessTools.Method(AccessTools.TypeByName(MetaDataType), "GetMetaData");
        var saveScreenshot = AccessTools.Method(AccessTools.TypeByName(PlatformType), "SaveScreenshot");
        if (getMetaData == null || saveScreenshot == null)
        {
            logError($"[Screenshots] The game's screenshot code has changed ({MetaDataType}.GetMetaData or " +
                     $"{PlatformType}.SaveScreenshot not found); screenshots will not be recorded.");
            return false;
        }

        harmony.Patch(getMetaData, postfix: new HarmonyMethod(typeof(ScreenshotPatches), nameof(GetMetaDataPostfix)));
        harmony.Patch(saveScreenshot,
            postfix: new HarmonyMethod(typeof(ScreenshotPatches), nameof(SaveScreenshotPostfix)));
        return true;
    }

    public static void GetMetaDataPostfix(object __instance, ref string __result)
    {
        try
        {
            // Counts down while a mega screenshot (several frames at a higher resolution) is being made.
            var mega = Traverse.Create(__instance).Field("doTakeMegaScreenshot").GetValue() is int frames && frames > 0;
            var exif = Mod?.OnBeforeSave(mega);
            if (exif != null) __result = exif;
        }
        catch (Exception e)
        {
            Mod?.LogError(e);
        }
    }

    public static void SaveScreenshotPostfix(string? path, string? filename, string __result)
    {
        try
        {
            Mod?.OnSaved(path, filename, __result);
        }
        catch (Exception e)
        {
            Mod?.LogError(e);
        }
    }
}
