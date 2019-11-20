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
        int GetTilesPerRow() => _contentRectangle.Width / _mapEditor.TileSize;

        void UpdateHoveredTileKind()
        {
            var newHoveredTileKindCoords = new Point(
                (int)MathF.Floor(((Desktop.MouseX - ViewRectangle.X)) / _mapEditor.TileSize),
                (int)MathF.Floor(((Desktop.MouseY - ViewRectangle.Y)) / _mapEditor.TileSize));

            // var hasHoveredTileChanged = _hoveredTileKindCoords != newHoveredTileKindCoords;
            _hoveredTileKindCoords = newHoveredTileKindCoords;
        }

        public override void OnMouseDown(int button, int clicks)
        {
            if (button == SDL.SDL_BUTTON_LEFT)
            {
                var hoveredTileIndex = (short)(_hoveredTileKindCoords.Y * GetTilesPerRow() + _hoveredTileKindCoords.X);
                if (hoveredTileIndex <= _mapEditor.TileKindsByLayer[_mapEditor.TileLayer].Length) _mapEditor.BrushTileKindIndex = hoveredTileIndex;
                else _mapEditor.BrushTileKindIndex = 0;
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

            if (_mapEditor.TileSetPath.Length == 0) return;

            var tileKinds = _mapEditor.TileKindsByLayer[_mapEditor.TileLayer];
            var tilesPerRow = GetTilesPerRow();

            // Empty tile
            {
                var rect = new SDL.SDL_Rect { x = _contentRectangle.X + 0 * _mapEditor.TileSize, y = _contentRectangle.Y + 0 * _mapEditor.TileSize, w = _mapEditor.TileSize, h = _mapEditor.TileSize };

                new Color(0xff0000ff).UseAsDrawColor(Desktop.Renderer);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.x, rect.y, rect.x + rect.w - 1, rect.y + rect.h - 1);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.x + rect.w - 1, rect.y, rect.x, rect.y + rect.h - 1);
            }

            for (var i = 0; i < tileKinds.Length; i++)
            {
                var x = (i + 1) % tilesPerRow;
                var y = (i + 1) / tilesPerRow;

                var spriteLocation = tileKinds[i].SpriteLocation;
                var sourceRect = new SDL.SDL_Rect { x = spriteLocation.X * _mapEditor.TileSize, y = spriteLocation.Y * _mapEditor.TileSize, w = _mapEditor.TileSize, h = _mapEditor.TileSize };
                var destRect = new SDL.SDL_Rect { x = _contentRectangle.X + x * _mapEditor.TileSize, y = _contentRectangle.Y + y * _mapEditor.TileSize, w = _mapEditor.TileSize, h = _mapEditor.TileSize };
                SDL.SDL_RenderCopy(Desktop.Renderer, _mapEditor.SpritesheetTexture, ref sourceRect, ref destRect);
            }

            // Selected tile
            {
                var x = _mapEditor.BrushTileKindIndex % tilesPerRow;
                var y = _mapEditor.BrushTileKindIndex / tilesPerRow;
                var outerRect = new SDL.SDL_Rect { x = _contentRectangle.X + x * _mapEditor.TileSize, y = _contentRectangle.Y + y * _mapEditor.TileSize, w = _mapEditor.TileSize, h = _mapEditor.TileSize };
                var midRect = new SDL.SDL_Rect { x = _contentRectangle.X + x * _mapEditor.TileSize + 1, y = _contentRectangle.Y + y * _mapEditor.TileSize + 1, w = _mapEditor.TileSize - 2, h = _mapEditor.TileSize - 2 };
                var innerRect = new SDL.SDL_Rect { x = _contentRectangle.X + x * _mapEditor.TileSize + 2, y = _contentRectangle.Y + y * _mapEditor.TileSize + 2, w = _mapEditor.TileSize - 4, h = _mapEditor.TileSize - 4 };

                new Color(0x000000ff).UseAsDrawColor(Desktop.Renderer);
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref outerRect);
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref innerRect);

                new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref midRect);
            }

            if (IsHovered)
            {
                var color = new Color(0x00ff00ff);
                color.UseAsDrawColor(Desktop.Renderer);

                var x = _hoveredTileKindCoords.X;
                var y = _hoveredTileKindCoords.Y;
                var w = 1;
                var h = 1;

                var renderX = ViewRectangle.X + (int)(x * _mapEditor.TileSize);
                var renderY = ViewRectangle.Y + (int)(y * _mapEditor.TileSize);

                var rect = new Rectangle(renderX, renderY, (int)(w * _mapEditor.TileSize), (int)(h * _mapEditor.TileSize)).ToSDL_Rect();
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref rect);
            }
        }
    }
}
