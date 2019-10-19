using SwarmBasics.Math;
using System;

namespace SwarmPlatform.Graphics
{
    public class TextureArea
    {
        public IntPtr Texture;
        public Rectangle Rectangle;

        public TextureArea(IntPtr texture, Rectangle area)
        {
            Texture = texture;
            Rectangle = area;
        }
    }
}
