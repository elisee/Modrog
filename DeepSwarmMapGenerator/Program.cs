using DeepSwarmCommon;
using SDL2;
using System;
using System.Runtime.InteropServices;
using static DeepSwarmCommon.Map;

namespace DeepSwarmMapGenerator
{
    class Program
    {

        static void Main()
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_CreateWindowAndRenderer(MapSize, MapSize, 0, out var window, out var renderer);

            var map = new Map();

            var mapTexture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGB888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, MapSize, MapSize);
            var mapRect = new SDL.SDL_Rect { x = 0, y = 0, w = MapSize, h = MapSize };

            MakeMap();

            var isRunning = true;

            while (isRunning)
            {
                while (isRunning && SDL.SDL_PollEvent(out var @event) != 0)
                {
                    switch (@event.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT: isRunning = false; break;
                        case SDL.SDL_EventType.SDL_WINDOWEVENT: if (@event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE) isRunning = false; break;
                        case SDL.SDL_EventType.SDL_KEYDOWN: if (@event.key.keysym.sym == SDL.SDL_Keycode.SDLK_RETURN) MakeMap(); break;
                    }
                }

                if (!isRunning) break;

                SDL.SDL_RenderClear(renderer);
                SDL.SDL_RenderCopy(renderer, mapTexture, ref mapRect, ref mapRect);
                SDL.SDL_RenderPresent(renderer);
            }


            void MakeMap()
            {
                map.Generate();

                SDL.SDL_LockTexture(mapTexture, IntPtr.Zero, out var pixels, out var pitch);

                unsafe
                {
                    for (var index = 0; index < map.Tiles.Length; index++)
                    {
                        uint color = Map.TileColors[(int)map.Tiles[index]];

                        Marshal.WriteByte(pixels + index * 4 + 0, (byte)(color >> 8));
                        Marshal.WriteByte(pixels + index * 4 + 1, (byte)(color >> 16));
                        Marshal.WriteByte(pixels + index * 4 + 2, (byte)(color >> 24));
                    }
                }

                SDL.SDL_UnlockTexture(mapTexture);
            }

        }
    }
}
