using ModrogCommon;
using SDL2;
using SwarmBasics;
using SwarmBasics.Math;
using SwarmPlatform.Graphics;
using SwarmPlatform.UI;
using System;

namespace ModrogEditor.Interface.Editing.Map
{
    class TileSelector : Panel
    {
        readonly MapEditor _mapEditor;
        Point _hoveredTileKindCoords;

        public TileSelector(MapEditor mapEditor, Element parent) : base(mapEditor.Desktop, parent)
        {
            _mapEditor = mapEditor;

            BackgroundPatch = new TexturePatch(0x000000ff);
        }

        #region Internals
        int GetTilesPerRow() => _contentRectangle.Width / Protocol.MapTileSize;

        void UpdateHoveredTileKind()
        {
            var newHoveredTileKindCoords = new Point(
                (int)MathF.Floor(((Desktop.MouseX - ViewRectangle.X)) / Protocol.MapTileSize),
                (int)MathF.Floor(((Desktop.MouseY - ViewRectangle.Y)) / Protocol.MapTileSize));

            // var hasHoveredTileChanged = _hoveredTileKindCoords != newHoveredTileKindCoords;
            _hoveredTileKindCoords = newHoveredTileKindCoords;
        }

        public override void OnMouseDown(int button)
        {
            if (button == SDL.SDL_BUTTON_LEFT)
            {
                _mapEditor.BrushTileIndex = (short)(1 + _hoveredTileKindCoords.Y * GetTilesPerRow() + _hoveredTileKindCoords.X);
            }
        }

        public override void OnMouseMove()
        {
            UpdateHoveredTileKind();
        }
        #endregion

        protected override void DrawSelf()
        {
            base.DrawSelf();

            var tileKinds = _mapEditor.TileKindsByLayer[_mapEditor.TileLayer];
            var tilesPerRow = GetTilesPerRow();

            for (var i = 0; i < tileKinds.Length; i++)
            {
                var x = i % tilesPerRow;
                var y = i / tilesPerRow;

                var spriteLocation = tileKinds[i].SpriteLocation;
                var sourceRect = new SDL.SDL_Rect { x = spriteLocation.X * Protocol.MapTileSize, y = spriteLocation.Y * Protocol.MapTileSize, w = Protocol.MapTileSize, h = Protocol.MapTileSize };
                var destRect = new SDL.SDL_Rect { x = _contentRectangle.X + x * Protocol.MapTileSize, y = _contentRectangle.Y + y * Protocol.MapTileSize, w = Protocol.MapTileSize, h = Protocol.MapTileSize };
                SDL.SDL_RenderCopy(Desktop.Renderer, _mapEditor.SpritesheetTexture, ref sourceRect, ref destRect);
            }

            if (IsHovered)
            {
                var color = new Color(0x00ff00ff);
                color.UseAsDrawColor(Desktop.Renderer);

                var x = _hoveredTileKindCoords.X;
                var y = _hoveredTileKindCoords.Y;
                var w = 1;
                var h = 1;

                var renderX = ViewRectangle.X + (int)(x * Protocol.MapTileSize);
                var renderY = ViewRectangle.Y + (int)(y * Protocol.MapTileSize);

                var rect = new Rectangle(renderX, renderY, (int)(w * Protocol.MapTileSize), (int)(h * Protocol.MapTileSize));

                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X, rect.Y, rect.X + rect.Width, rect.Y);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X + rect.Width, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X + rect.Width, rect.Y + rect.Height, rect.X, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X, rect.Y + rect.Height, rect.X, rect.Y);
            }
        }
    }
}
