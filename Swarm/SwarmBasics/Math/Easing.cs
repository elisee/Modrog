using System;

namespace SwarmBasics.Math
{
    public static class Easing
    {
        public static float InSin(float t) => 1 + MathF.Sin(MathF.PI / 2f * t - MathF.PI / 2f);
        public static float OutSin(float t) => MathF.Sin(MathF.PI / 2f * t);
        public static float InOutSin(float t) => (1 + MathF.Sin(MathF.PI * t - MathF.PI / 2f)) / 2f;

        public static float InQuad(float t) => t * t;
        public static float OutQuad(float t) => t * (2 - t);
        public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

        public static float InCubic(float t) => t * t * t;
        public static float OutCubic(float t) => (t - 1f) * (t - 1f) * (t - 1f) + 1f;
        public static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f;
    }
}
