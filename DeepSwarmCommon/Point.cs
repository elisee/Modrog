using System;

namespace DeepSwarmCommon
{
    public struct Point : IEquatable<Point>
    {
        public static readonly Point Zero = new Point();

        public int X;
        public int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj) => obj is Point point && Equals(point);
        public bool Equals(Point other) => X == other.X && Y == other.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(Point left, Point right) => left.Equals(right);
        public static bool operator !=(Point left, Point right) => !(left == right);
    }
}
