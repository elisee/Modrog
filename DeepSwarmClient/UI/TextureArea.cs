using DeepSwarmBasics.Math;
using System;

namespace DeepSwarmClient.UI
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
