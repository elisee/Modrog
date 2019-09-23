using DeepSwarmCommon;
using SDL2;
using System;
using System.IO;
using static DeepSwarmCommon.Map;

namespace DeepSwarmMapGenerator
{
    class Program
    {

        static void Main()
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_CreateWindowAndRenderer(MapSize, MapSize, 0, out var window, out var renderer);

            if (SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG) != (int)SDL_image.IMG_InitFlags.IMG_INIT_PNG) throw new Exception();

            var assetsPath = FileHelper.FindAppFolder("Assets");
            var spritesheetTexture = SDL_image.IMG_LoadTexture(renderer, Path.Combine(assetsPath, "Spritesheet.png"));

            var map = new Map();
            map.Generate();

            var isRunning = true;

            while (isRunning)
            {
                while (isRunning && SDL.SDL_PollEvent(out var @event) != 0)
                {
                    switch (@event.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT: isRunning = false; break;
                        case SDL.SDL_EventType.SDL_WINDOWEVENT: if (@event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE) isRunning = false; break;
                        case SDL.SDL_EventType.SDL_KEYDOWN: if (@event.key.keysym.sym == SDL.SDL_Keycode.SDLK_RETURN) map.Generate(); break;
                    }
                }

                if (!isRunning) break;

                SDL.SDL_RenderClear(renderer);

                for (var y = 0; y < MapSize; y++)
                {
                    for (var x = 0; x < MapSize; x++)
                    {
                        var index = y * MapSize + x;
                        var tile = (int)map.Tiles[index];

                        var sourceRect = new SDL.SDL_Rect { x = TileSize * (5 + tile), y = 0, w = 1, h = 1 };
                        var destRect = new SDL.SDL_Rect { x = x, y = y, w = 1, h = 1 };
                        SDL.SDL_RenderCopy(renderer, spritesheetTexture, ref sourceRect, ref destRect);
                    }
                }

                SDL.SDL_RenderPresent(renderer);
            }
        }
    }
}
