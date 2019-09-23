using DeepSwarmClient.UI;
using SDL2;
using System;

namespace DeepSwarmClient
{
    static class RendererHelper
    {
        public static IntPtr FontTexture;
        public const int FontSourceSize = 8;
        public const int FontRenderSize = 16;

        public static void DrawText(IntPtr renderer, int x, int y, string text, Color color)
        {
            SDL.SDL_SetTextureColorMod(FontTexture, color.R, color.G, color.B);

            for (var i = 0; i < text.Length; i++)
            {
                var index = text[i] - 32;
                var column = index % 15;
                var row = index / 15;

                var sourceRect = new SDL.SDL_Rect { x = column * FontSourceSize, y = row * FontSourceSize, w = FontSourceSize, h = FontSourceSize };
                var destRect = new SDL.SDL_Rect { x = x + i * FontRenderSize, y = y, w = FontRenderSize, h = FontRenderSize };
                SDL.SDL_RenderCopy(renderer, FontTexture, ref sourceRect, ref destRect);
            }
        }
    }
}
