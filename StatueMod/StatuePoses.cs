namespace Nf3t.VintageStory.Statue;

/// <summary>
/// The player's pose and the elk's pose are chosen separately. The player stands, does an emote, or rides; when the
/// player rides, the game's riding animation that matches the elk's gait is used, so rider and elk stay in step
/// (same animation names and frame counts in the game, so one frame number serves both).
/// </summary>
public static class StatuePoses
{
    /// <summary>Player pose: just standing (or, on an elk, sitting still).</summary>
    public const string Standing = "none";

    /// <summary>Player pose: riding. On an elk the rider follows the elk's gait.</summary>
    public const string Riding = "riding";

    public const string ElkStand = "stand";
    public const string ElkWalk = "walk";
    public const string ElkGallop = "gallop";

    public static readonly string[] ElkPoses = [ElkStand, ElkWalk, ElkGallop];

    /// <summary>The player poses that are not emotes from the game's player definition.</summary>
    public static bool IsKnown(string pose) => pose is Standing or Riding;

    public static bool IsKnownElkPose(string elkPose) => System.Array.IndexOf(ElkPoses, elkPose) >= 0;

    /// <summary>The elk animation to freeze, or null for the elk standing still (its bind pose).</summary>
    public static string? ElkAnimation(string elkPose) => elkPose switch
    {
        ElkWalk => "walk",
        ElkGallop => "run",
        _ => null,
    };

    /// <summary>The seraph animation of someone riding an elk that moves like this.</summary>
    public static string RiderAnimation(string elkPose) => elkPose switch
    {
        ElkWalk => "rideelk-walk",
        ElkGallop => "rideelk-run",
        _ => "rideelk-idle",
    };
}
