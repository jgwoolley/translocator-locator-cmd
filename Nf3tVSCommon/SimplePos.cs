namespace Nf3t.VintageStory.Common;

public readonly record struct SimplePos(int X, int Y, int Z)
{
    public double DistanceTo(SimplePos other)
    {
        return Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2) + Math.Pow(Z - other.Z, 2));
    }

    public string ToRelativeString(SimplePos defaultSpawnPosition)
    {
        return $"{X - defaultSpawnPosition.X}, {Y}, {Z - defaultSpawnPosition.Z}";
    }

    public string GetDirectionArrow(SimplePos playerPos)
    {
        // Calculate difference (Target - Player)
        double dz = Z - playerPos.Z;
        double dx = X - playerPos.X;

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

    public string ToRelativeString(SimplePos defaultSpawnPosition, SimplePos playerPosition)
    {
        var distance = (int)DistanceTo(playerPosition);
        var arrow = GetDirectionArrow(playerPosition);

        return $"{ToRelativeString(defaultSpawnPosition)} ({distance}m {arrow})";
    }
}