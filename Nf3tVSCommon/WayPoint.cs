using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Common;

public class WayPoint
{
    [JsonConstructor]
    public WayPoint(string codePath, BlockPos pos, string name, string icon, string color)
    {
        CodePath = codePath;
        Pos = pos;
        Name = name;
        Icon = icon;
        Color = color;
        ExtraChat = "";
        BlockCount = 1;
    }

    [JsonProperty] public string CodePath { get; }

    [JsonProperty] public BlockPos Pos { get; }
    [JsonProperty] public string Name { get; }

    // https://wiki.vintagestory.at/VTML
    [JsonProperty] public string Icon { get; }
    [JsonProperty] public string Color { get; }
    [JsonProperty] public string ExtraChat { get; set; }
    [JsonProperty] public int BlockCount { get; set; }

    public double DistanceTo(BlockPos other)
    {
        return Math.Sqrt(Pos.DistanceSqTo(other.X, other.Y, other.Z));
    }

    public string ToWaypointString()
    {
        return $"/waypoint addati {Icon} ={Pos.X} ={Pos.Y} ={Pos.Z} false {Color} \"{Name}\"";
    }

    public string GetDirectionArrow(BlockPos playerPos)
    {
        // Calculate difference (Target - Player)
        double dz = Pos.Z - playerPos.Z;
        double dx = Pos.X - playerPos.X;

        // Atan2 returns the angle in radians
        // Math.Atan2(y, x) -> we use Z as Y for the 2D plane
        var radians = Math.Atan2(dz, dx);
        var degrees = radians * (180 / Math.PI);

        // Normalize to 0-360 for easier mapping
        // 0 is East, 90 is South, 180 is West, 270 is North
        var angle = (degrees + 360) % 360;

        if (angle >= 337.5 || angle < 22.5) return "→"; // East
        if (angle >= 22.5 && angle < 67.5) return "↘"; // South-East
        if (angle >= 67.5 && angle < 112.5) return "↓"; // South
        if (angle >= 112.5 && angle < 157.5) return "↙"; // South-West
        if (angle >= 157.5 && angle < 202.5) return "←"; // West
        if (angle >= 202.5 && angle < 247.5) return "↖"; // North-West
        if (angle >= 247.5 && angle < 292.5) return "↑"; // North
        if (angle >= 292.5 && angle < 337.5) return "↗"; // North-East

        return "•";
    }

    public string ToRelativeCoordinates(BlockPos mapMiddlePos, BlockPos playerPos)
    {
        var distance = (int)DistanceTo(playerPos);
        var arrow = GetDirectionArrow(playerPos);
        var relativeX = Pos.X - mapMiddlePos.X;
        var relativeZ = Pos.Z - mapMiddlePos.Z;

        return $"{relativeX}, {Pos.Y}, {relativeZ} ({distance}m {arrow})";
    }

    public string ToChatString(BlockPos mapMiddlePos, BlockPos playerPos)
    {
        var prefix = "";
        if (BlockCount > 1)
        {
            prefix += "x";
            prefix += BlockCount;
            prefix += " ";
        }

        return $"<font color=\"{Color}\">{prefix}[{Name}]</font> " +
               $"at <strong>{ToRelativeCoordinates(mapMiddlePos, playerPos)}{ExtraChat}</strong>";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not WayPoint other) return false;
        // We define a duplicate by its position. 
        // Usually, you don't want two waypoints at the exact same spot.
        return Pos.X == other.Pos.X && Pos.Y == other.Pos.Y && Pos.Z == other.Pos.Z;
    }

    public override int GetHashCode()
    {
        // Only hash the Position, as that is our "unique key"
        return HashCode.Combine(Pos.X, Pos.Y, Pos.Z);
    }
}