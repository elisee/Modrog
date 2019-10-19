using SDL2;
using System;

namespace SwarmPlatform.Graphics
{
    public static class Cursors
    {
        public readonly static IntPtr ArrowCursor = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);
        public readonly static IntPtr HandCursor = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_HAND);
        public readonly static IntPtr IBeamCursor = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_IBEAM);
    }
}
