namespace DeepSwarmClient.UI
{
    public class TexturePatch
    {
        public TextureArea TextureArea;
        public int HorizontalBorder;
        public int VerticalBorder;
        public Color Color = Color.White;

        public TexturePatch(TextureArea textureArea)
        {
            TextureArea = textureArea;
        }

        public TexturePatch(uint color)
        {
            Color = new Color(color);
        }
    }
}
