using System;
using System.Diagnostics;
using System.Globalization;

namespace SwarmBasics.Math
{
    [DebuggerDisplay("{DebugDisplay,nq}")]
    public struct Vector2 : IEquatable<Vector2>
    {
        public static readonly Vector2 Zero = new Vector2();
        public static readonly Vector2 One = new Vector2(1f, 1f);

        public float X;
        public float Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj) => obj is Vector2 vector && Equals(vector);
        public bool Equals(Vector2 other) => X == other.X && Y == other.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(Vector2 left, Vector2 right) => left.Equals(right);
        public static bool operator !=(Vector2 left, Vector2 right) => !(left == right);
        public static Vector2 operator +(Vector2 left, Vector2 right) => new Vector2(left.X + right.X, left.Y + right.Y);
        public static Vector2 operator -(Vector2 left, Vector2 right) => new Vector2(left.X - right.X, left.Y - right.Y);
        public static Vector2 operator *(Vector2 vector, float value) => new Vector2(vector.X * value, vector.Y * value);

        public static Vector2 operator -(Vector2 vector) => new Vector2(-vector.X, -vector.Y);

        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => new Vector2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

        internal string DebugDisplay => $"{X.ToString(CultureInfo.InvariantCulture)} {Y.ToString(CultureInfo.InvariantCulture)}";
    }
}
