namespace Nf3t.VintageStory.Screenshots;

/// <summary>Stored in <c>ModConfig/nf3tscreenshots.json</c>.</summary>
public class ScreenshotsConfig
{
    /// <summary>Also write the location, world and server into each image's EXIF data. Off by default: anyone you
    /// share the image with could read where it was taken and on which server. Never written on worlds that hide
    /// coordinates.</summary>
    public bool WriteExif { get; set; } = false;

    /// <summary>Also record timelapse frames (<c>.timelapse</c>), which go to their own folder.</summary>
    public bool IndexTimelapse { get; set; } = false;
}
