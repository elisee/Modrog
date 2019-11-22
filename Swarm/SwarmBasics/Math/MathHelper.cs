using System;

namespace SwarmBasics.Math
{
    public static class MathHelper
    {
        public static float WrapAngle(float angle)
        {
            while (angle < -MathF.PI) angle += MathF.PI * 2f;
            while (angle > MathF.PI) angle -= MathF.PI * 2f;
            return angle;
        }

        public static int Mod(int dividend, int divisor)
        {
            var remainder = dividend % divisor;
            return remainder >= 0 ? remainder : remainder + divisor;
        }

        public static float ToDegrees(float angle) => angle * 180f / MathF.PI;
    }
}
