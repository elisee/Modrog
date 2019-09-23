using DeepSwarmClient.UI;
using DeepSwarmCommon;
using SDL2;
using System;

namespace DeepSwarmClient
{
    class InGameView : EngineElement
    {
        readonly Element _playerListPopup;

        public InGameView(Engine engine)
            : base(engine, null)
        {
            AnchorRectangle = engine.Viewport;

            _playerListPopup = new Element(Desktop, null)
            {
                AnchorRectangle = new Rectangle(0, 0, (Protocol.MaxPlayerNameLength + 2) * 16, 720),
                BackgroundColor = new Color(0x123456ff)
            };
        }

        public override void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            if (repeat) return;

            if (key == SDL.SDL_Keycode.SDLK_TAB)
            {
                Add(_playerListPopup);
                _playerListPopup.Layout(_layoutRectangle);
            }

            if (key == SDL.SDL_Keycode.SDLK_a || key == SDL.SDL_Keycode.SDLK_q) Engine.IsScrollingLeft = true;
            if (key == SDL.SDL_Keycode.SDLK_d) Engine.IsScrollingRight = true;
            if (key == SDL.SDL_Keycode.SDLK_w || key == SDL.SDL_Keycode.SDLK_z) Engine.IsScrollingUp = true;
            if (key == SDL.SDL_Keycode.SDLK_s) Engine.IsScrollingDown = true;
        }

        public override void OnKeyUp(SDL.SDL_Keycode key)
        {
            if (key == SDL.SDL_Keycode.SDLK_TAB)
            {
                Remove(_playerListPopup);
            }

            if (key == SDL.SDL_Keycode.SDLK_a || key == SDL.SDL_Keycode.SDLK_q) Engine.IsScrollingLeft = false;
            if (key == SDL.SDL_Keycode.SDLK_d) Engine.IsScrollingRight = false;
            if (key == SDL.SDL_Keycode.SDLK_w || key == SDL.SDL_Keycode.SDLK_z) Engine.IsScrollingUp = false;
            if (key == SDL.SDL_Keycode.SDLK_s) Engine.IsScrollingDown = false;
        }

        public void OnPlayerListUpdated()
        {
            _playerListPopup.Clear();

            for (var i = 0; i < Engine.PlayerList.Count; i++)
            {
                var entry = Engine.PlayerList[i];
                var label = new Label(Desktop, _playerListPopup) { Text = $"[{entry.Team.ToString()}] {entry.Name}{(entry.IsOnline ? "" : " (offline)")}" };
                label.AnchorRectangle = new Rectangle(16, 16 + 16 * i, _playerListPopup.AnchorRectangle.Width, 16);
            }

            _playerListPopup.Layout(_layoutRectangle);
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            DrawMap();
        }

        void DrawMap()
        {
            var startTileX = (int)Engine.ScrollingPixelsX / Map.TileSize;
            var startTileY = (int)Engine.ScrollingPixelsY / Map.TileSize;

            var tilesPerRow = (int)MathF.Ceiling((float)Engine.Viewport.Width / Map.TileSize + 1);
            var tilesPerColumn = (int)MathF.Ceiling((float)Engine.Viewport.Height / Map.TileSize + 1);

            var tileViewport = new Rectangle(startTileX, startTileY, tilesPerRow, tilesPerColumn);

            for (var y = 0; y < tilesPerColumn; y++)
            {
                for (var x = 0; x < tilesPerRow; x++)
                {
                    var index = (startTileY + y) * Map.MapSize + (startTileX + x);

                    var color = new Color(Map.TileColors[(int)Engine.Map.Tiles[index]]);
                    color.UseAsDrawColor(Engine.Renderer);

                    var rect = new SDL.SDL_Rect
                    {
                        x = (startTileX + x) * Map.TileSize - (int)Engine.ScrollingPixelsX,
                        y = (startTileY + y) * Map.TileSize - (int)Engine.ScrollingPixelsY,
                        w = Map.TileSize,
                        h = Map.TileSize
                    };

                    SDL.SDL_RenderFillRect(Engine.Renderer, ref rect);
                }
            }

            foreach (var entity in Engine.Map.Entities)
            {
                if (!tileViewport.Contains(entity.X, entity.Y)) continue;

                var stats = Entity.EntityStatsByType[(int)entity.Type];
                var color = new Color(stats.NeutralColor);
                if (entity.PlayerIndex != -1) color.RGBA = Engine.PlayerList[entity.PlayerIndex].Team == Player.PlayerTeam.Blue ? stats.BlueColor : stats.RedColor;

                color.UseAsDrawColor(Engine.Renderer);

                var rect = new SDL.SDL_Rect
                {
                    x = (entity.X) * Map.TileSize - (int)Engine.ScrollingPixelsX,
                    y = (entity.Y) * Map.TileSize - (int)Engine.ScrollingPixelsY,
                    w = Map.TileSize,
                    h = Map.TileSize
                };

                SDL.SDL_RenderFillRect(Engine.Renderer, ref rect);
            }
        }
    }
}
