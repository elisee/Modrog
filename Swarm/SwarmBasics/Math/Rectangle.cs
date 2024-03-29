﻿using System;
using System.Diagnostics;

namespace SwarmBasics.Math
{
    [DebuggerDisplay("{DebugDisplay,nq}")]
    public struct Rectangle : IEquatable<Rectangle>
    {
        public static readonly Rectangle Zero = new Rectangle(0, 0, 0, 0);

        public int X;
        public int Y;
        public int Width;
        public int Height;

        public int Bottom => Y + Height;
        public int Top => Y;
        public int Left => X;
        public int Right => X + Width;

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Contains(int x, int y) => x >= X && y >= Y && x < X + Width && y < Y + Height;
        public bool Contains(Point point) => Contains(point.X, point.Y);

        public override bool Equals(object obj) => obj is Rectangle rectangle && Equals(rectangle);
        public bool Equals(Rectangle other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
        public static bool operator ==(Rectangle left, Rectangle right) => left.Equals(right);
        public static bool operator !=(Rectangle left, Rectangle right) => !(left == right);

        public static Rectangle operator +(Rectangle rectangle, Point offset) => new Rectangle(rectangle.X + offset.X, rectangle.Y + offset.Y, rectangle.Width, rectangle.Height);
        public static Rectangle operator -(Rectangle rectangle, Point offset) => new Rectangle(rectangle.X - offset.X, rectangle.Y - offset.Y, rectangle.Width, rectangle.Height);

        internal string DebugDisplay => $"{X} {Y} {Width} {Height}";
    }
}
