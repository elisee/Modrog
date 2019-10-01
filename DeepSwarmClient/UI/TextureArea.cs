using DeepSwarmCommon;
using System;

namespace DeepSwarmClient.UI
{
    public class TextureArea
    {
        public IntPtr Texture;
        public Rectangle Area;

        public TextureArea(IntPtr texture, Rectangle area)
        {
            Texture = texture;
            Area = area;
        }
    }
}
