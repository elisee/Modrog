using SwarmBasics.Math;
using System;

namespace ModrogApi
{
    public static class MathHelper
    {
        public static Point GetOffsetFromDirection(Direction direction) => direction switch
        {
            Direction.Right => new Point(1, 0),
            Direction.Down => new Point(0, 1),
            Direction.Left => new Point(-1, 0),
            Direction.Up => new Point(0, -1),
            _ => throw new InvalidOperationException(),
        };

        public static float GetAngleFromDirection(Direction direction) => direction switch
        {
            Direction.Right => 0f,
            Direction.Down => MathF.PI / 2f,
            Direction.Left => MathF.PI,
            Direction.Up => MathF.PI * 3f / 2f,
            _ => throw new InvalidOperationException(),
        };
    }
}
