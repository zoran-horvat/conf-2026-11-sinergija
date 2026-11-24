namespace MercuryOutpost.Web.Outpost;

public enum Direction
{
    North,
    South,
    East,
    West,
}

public static class DirectionExtensions
{
    public static Location Step(this Location location, Direction direction) => direction switch
    {
        Direction.North => location with { Y = location.Y - 1 },
        Direction.South => location with { Y = location.Y + 1 },
        Direction.East => location with { X = location.X + 1 },
        Direction.West => location with { X = location.X - 1 },
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };
}

