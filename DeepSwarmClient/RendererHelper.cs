using DeepSwarmClient.UI;
using DeepSwarmCommon;
using SDL2;
using System;

namespace DeepSwarmClient
{
    static class RendererHelper
    {
        public readonly static IntPtr ArrowCursor = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);
        public readonly static IntPtr HandCursor = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_HAND);
        public readonly static IntPtr IbeamCursor = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_IBEAM);

        #region Text
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
        #endregion

        #region Map
        enum TileRenderType { Single, Connected }

        struct TileRenderDefinition
        {
            public TileRenderType Type;
            public Point Position;

            public TileRenderDefinition(TileRenderType type, Point position)
            {
                Type = type;
                Position = position;
            }
        }

        readonly static TileRenderDefinition[] TileRenderDefinitionsByTile = new TileRenderDefinition[]
        {
            new TileRenderDefinition(TileRenderType.Single, new Point(5, 0)),    // Unknown
            new TileRenderDefinition(TileRenderType.Single, new Point(6, 0)),    // Rock
            new TileRenderDefinition(TileRenderType.Single, new Point(7, 0)),   // Path
            new TileRenderDefinition(TileRenderType.Connected, new Point(7, 1)), // Dirt1
            new TileRenderDefinition(TileRenderType.Single, new Point(8, 0)),    // Dirt2
            new TileRenderDefinition(TileRenderType.Single, new Point(9, 0)),    // Dirt3
            new TileRenderDefinition(TileRenderType.Single, new Point(10, 0)),   // Crystal1
            new TileRenderDefinition(TileRenderType.Single, new Point(11, 0)),   // Crystal2
            new TileRenderDefinition(TileRenderType.Single, new Point(12, 0)),   // Crystal3
            new TileRenderDefinition(TileRenderType.Single, new Point(13, 0)),   // Crystal4
            new TileRenderDefinition(TileRenderType.Single, new Point(14, 0)),   // Crystal5
        };

        enum TileRenderConnections
        {
            None = 0,
            Left = 1,
            Right = 2,
            Down = 4,
            Up = 8
        }

        readonly static Point[] AutotilingOffsets = new Point[]
        {
            new Point(3, 3),
            new Point(2, 3),
            new Point(0, 3),
            new Point(1, 3),
            new Point(3, 0),
            new Point(2, 0),
            new Point(0, 0),
            new Point(1, 0),
            new Point(3, 2),
            new Point(2, 2),
            new Point(0, 2),
            new Point(1, 2),
            new Point(3, 1),
            new Point(2, 1),
            new Point(0, 1),
            new Point(1, 1),
        };

        public static SDL.SDL_Rect GetTileRenderSourceRect(int x, int y, Map map)
        {
            var tile = map.PeekTile(x, y);

            var tileRenderDefinition = TileRenderDefinitionsByTile[(int)tile];
            var tilePosition = tileRenderDefinition.Position;

            if (tileRenderDefinition.Type == TileRenderType.Connected)
            {
                var autotileConnectionFlag = TileRenderConnections.None;
                var leftTile = map.PeekTile(x - 1, y);
                if (leftTile == tile || leftTile == Map.Tile.Unknown) autotileConnectionFlag |= TileRenderConnections.Left;
                var rightTile = map.PeekTile(x + 1, y);
                if (rightTile == tile || rightTile == Map.Tile.Unknown) autotileConnectionFlag |= TileRenderConnections.Right;
                var downTile = map.PeekTile(x, y + 1);
                if (downTile == tile || downTile == Map.Tile.Unknown) autotileConnectionFlag |= TileRenderConnections.Down;
                var upTile = map.PeekTile(x, y - 1);
                if (upTile == tile || upTile == Map.Tile.Unknown) autotileConnectionFlag |= TileRenderConnections.Up;

                tilePosition += AutotilingOffsets[(int)autotileConnectionFlag];
            }

            return new SDL.SDL_Rect() { x = tilePosition.X * Map.TileSize, y = tilePosition.Y * Map.TileSize, w = Map.TileSize, h = Map.TileSize };
        }
        #endregion

        #region Entities
        public static void GetEntityRenderRects(Entity entity, Player.PlayerTeam entityTeam, out SDL.SDL_Rect sourceRect, out SDL.SDL_Rect destRect)
        {
            switch (entity.Type)
            {
                case Entity.EntityType.Factory:
                    {
                        sourceRect = Desktop.ToSDL_Rect(new Rectangle(0, 0, 24 * 3, 24 * 3));
                        destRect = Desktop.ToSDL_Rect(new Rectangle(-24, -24, 24 * 3, 24 * 3));

                        break;
                    }

                case Entity.EntityType.Heart:
                    {
                        var teamOffset = entityTeam == Player.PlayerTeam.Blue ? 0 : 1;

                        sourceRect = Desktop.ToSDL_Rect(new Rectangle(24 * (3 + teamOffset), 0, 24, 24));
                        destRect = Desktop.ToSDL_Rect(new Rectangle(0, 0, 24, 24));

                        break;
                    }

                case Entity.EntityType.Robot:
                    {
                        var teamOffset = entityTeam == Player.PlayerTeam.Blue ? 0 : 1;

                        switch (entity.Direction)
                        {
                            case Entity.EntityDirection.Left:
                                sourceRect = Desktop.ToSDL_Rect(new Rectangle(0, 24 * (3 + teamOffset * 3), 24 * 2, 24 * 3));
                                destRect = Desktop.ToSDL_Rect(new Rectangle(-24, -24, 24 * 2, 24 * 3));
                                break;

                            case Entity.EntityDirection.Down:
                                sourceRect = Desktop.ToSDL_Rect(new Rectangle(24 * 2, 24 * (3 + teamOffset * 3), 24, 24 * 3));
                                destRect = Desktop.ToSDL_Rect(new Rectangle(0, -24, 24, 24 * 3));
                                break;

                            case Entity.EntityDirection.Up:
                                sourceRect = Desktop.ToSDL_Rect(new Rectangle(24 * 3, 24 * (3 + teamOffset * 3), 24, 24 * 3));
                                destRect = Desktop.ToSDL_Rect(new Rectangle(0, -24, 24, 24 * 3));
                                break;

                            case Entity.EntityDirection.Right:
                                sourceRect = Desktop.ToSDL_Rect(new Rectangle(24 * 4, 24 * (3 + teamOffset * 3), 24 * 2, 24 * 3));
                                destRect = Desktop.ToSDL_Rect(new Rectangle(0, -24, 24 * 2, 24 * 3));
                                break;

                            default: throw new NotSupportedException();
                        }

                        break;
                    }

                default: throw new Exception();
            }
        }
        #endregion
    }
}
