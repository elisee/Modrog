using DeepSwarmBasics;
using DeepSwarmBasics.Math;
using SDL2;
using System;

namespace DeepSwarmClient.Graphics
{
    static class SDLExtensions
    {
        public static SDL.SDL_Rect ToSDL_Rect(this Rectangle rectangle)
        {
            return new SDL.SDL_Rect { x = rectangle.X, y = rectangle.Y, w = rectangle.Width, h = rectangle.Height };
        }

        public static void UseAsDrawColor(this Color color, IntPtr renderer) => SDL.SDL_SetRenderDrawColor(renderer, color.R, color.G, color.B, color.A);
    }
}
