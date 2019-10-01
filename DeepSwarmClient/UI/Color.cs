using SDL2;
using System;

namespace DeepSwarmClient.UI
{
    public struct Color
    {
        public static readonly Color White = new Color(0xffffffff);

        public uint RGBA;

        public Color(uint rgba)
        {
            RGBA = rgba;
        }

        public byte R => (byte)((RGBA >> 24) & 0xff);
        public byte G => (byte)((RGBA >> 16) & 0xff);
        public byte B => (byte)((RGBA >> 8) & 0xff);
        public byte A => (byte)((RGBA >> 0) & 0xff);

        public void UseAsDrawColor(IntPtr renderer) => SDL.SDL_SetRenderDrawColor(renderer, R, G, B, A);
    }
}
