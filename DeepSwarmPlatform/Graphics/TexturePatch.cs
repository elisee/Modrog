using DeepSwarmBasics;
using DeepSwarmBasics.Math;
using SDL2;
using System;

namespace DeepSwarmPlatform.Graphics
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

        public void Draw(IntPtr Renderer, Rectangle rectangle)
        {
            if (Color.A != byte.MaxValue) SDL.SDL_SetRenderDrawBlendMode(Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

            Color.UseAsDrawColor(Renderer);

            if (TextureArea == null)
            {
                var rect = rectangle.ToSDL_Rect();
                SDL.SDL_RenderFillRect(Renderer, ref rect);
            }
            else
            {
                // TODO: Support HorizontalBorder and VerticalBorder
                var sourceRect = TextureArea.Rectangle.ToSDL_Rect();
                var destRect = rectangle.ToSDL_Rect();
                SDL.SDL_RenderCopy(Renderer, TextureArea.Texture, ref sourceRect, ref destRect);
            }

            if (Color.A != byte.MaxValue) SDL.SDL_SetRenderDrawBlendMode(Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);
        }
    }
}
