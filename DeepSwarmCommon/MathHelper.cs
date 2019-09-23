using System;

namespace DeepSwarmCommon
{
    public static class MathHelper
    {
        public static float WrapAngle(float angle)
        {
            while (angle < -MathF.PI) angle += MathF.PI * 2f;
            while (angle > MathF.PI) angle -= MathF.PI * 2f;
            return angle;
        }
    }
}
