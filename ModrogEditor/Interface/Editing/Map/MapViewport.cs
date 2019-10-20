﻿using SwarmBasics;
using SwarmBasics.Math;
using ModrogCommon;
using SwarmPlatform.Graphics;
using SwarmPlatform.UI;
using SDL2;
using System;
using System.Collections.Generic;

namespace ModrogEditor.Interface.Editing.Map
{
    class MapViewport : Panel
    {
        readonly MapEditor _mapEditor;

        // Chunks
        class Chunk
        {
            public readonly short[,] TilesPerLayer;
            public Chunk() { TilesPerLayer = new short[(int)Protocol.MapLayer.Count, Protocol.MapChunkSide * Protocol.MapChunkSide]; }
        }

        readonly Dictionary<Point, Chunk> _chunks = new Dictionary<Point, Chunk>();

        // Scrolling
        Vector2 _scroll;
        float _zoom = 2f;
        const float MinZoom = 0.5f;
        const float MaxZoom = 2f;

        bool _isScrollingLeft;
        bool _isScrollingRight;
        bool _isScrollingUp;
        bool _isScrollingDown;

        bool _isDraggingScroll;
        Vector2 _dragScroll;

        // Tiles
        Point _hoveredTile;
        bool _isPlacingTiles;

        public MapViewport(MapEditor mapEditor, Element parent) : base(mapEditor.Desktop, parent)
        {
            _mapEditor = mapEditor;
            BackgroundPatch = new TexturePatch(0x000000ff);
        }

        #region Internals
        public override bool AcceptsFocus() => true;

        public override Element HitTest(int x, int y)
        {
            return base.HitTest(x, y) ?? (ViewRectangle.Contains(x, y) ? this : null);
        }

        void UpdateHoveredTile()
        {
            var viewportScroll = new Vector2(
                _scroll.X - ViewRectangle.Width / 2 / _zoom,
                _scroll.Y - ViewRectangle.Height / 2 / _zoom);

            var newHoveredTile = new Point(
                (int)MathF.Floor((viewportScroll.X + (Desktop.MouseX - ViewRectangle.X) / _zoom) / Protocol.MapTileSize),
                (int)MathF.Floor((viewportScroll.Y + (Desktop.MouseY - ViewRectangle.Y) / _zoom) / Protocol.MapTileSize));

            var hasHoveredTileChanged = _hoveredTile != newHoveredTile;
            _hoveredTile = newHoveredTile;

            if (hasHoveredTileChanged)
            {
                if (_isPlacingTiles) PutTile();
            }
        }

        void PutTile()
        {
            var chunkCoords = new Point(
                (int)MathF.Floor((float)_hoveredTile.X / Protocol.MapChunkSide),
                (int)MathF.Floor((float)_hoveredTile.Y / Protocol.MapChunkSide));

            if (!_chunks.TryGetValue(chunkCoords, out var chunk))
            {
                chunk = new Chunk();
                _chunks.Add(chunkCoords, chunk);
            }

            var chunkTileCoords = new Point(
                MathHelper.Mod(_hoveredTile.X, Protocol.MapChunkSide),
                MathHelper.Mod(_hoveredTile.Y, Protocol.MapChunkSide));

            switch (_mapEditor.Tool)
            {
                case MapEditor.MapEditorTool.Brush:
                    chunk.TilesPerLayer[_mapEditor.TileLayer, chunkTileCoords.Y * Protocol.MapChunkSide + chunkTileCoords.X] = _mapEditor.BrushTileIndex;
                    break;
            }
        }

        short GetTileAt(Point position)
        {
            var chunkCoords = new Point(
                (int)MathF.Floor((float)position.X / Protocol.MapChunkSide),
                (int)MathF.Floor((float)position.Y / Protocol.MapChunkSide));

            if (!_chunks.TryGetValue(chunkCoords, out var chunk)) return 0;

            var chunkTileCoords = new Point(
                MathHelper.Mod(position.X, Protocol.MapChunkSide),
                MathHelper.Mod(position.Y, Protocol.MapChunkSide));

            return chunk.TilesPerLayer[_mapEditor.TileLayer, chunkTileCoords.Y * Protocol.MapChunkSide + chunkTileCoords.X];
        }


        public override void OnMounted()
        {
            Desktop.RegisterAnimation(Animate);
            Desktop.SetFocusedElement(this);
        }

        public override void OnUnmounted()
        {
            // TODO:
            /* if (SpritesheetTexture != IntPtr.Zero)
            {
                SDL.SDL_DestroyTexture(SpritesheetTexture);
                SpritesheetTexture = IntPtr.Zero;
            } */

            Desktop.UnregisterAnimation(Animate);
        }

        public override void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            base.OnKeyDown(key, repeat);

            if (!repeat && !_isDraggingScroll)
            {
                if (key == SDL.SDL_Keycode.SDLK_KP_PLUS) _zoom = Math.Min(_zoom + 0.1f, MaxZoom);
                if (key == SDL.SDL_Keycode.SDLK_KP_MINUS) _zoom = Math.Max(_zoom - 0.1f, MinZoom);

                if (key == SDL.SDL_Keycode.SDLK_LEFT) _isScrollingLeft = true;
                if (key == SDL.SDL_Keycode.SDLK_RIGHT) _isScrollingRight = true;
                if (key == SDL.SDL_Keycode.SDLK_UP) _isScrollingUp = true;
                if (key == SDL.SDL_Keycode.SDLK_DOWN) _isScrollingDown = true;

                if (key == SDL.SDL_Keycode.SDLK_n)
                {
                    _mapEditor.SetBrush(tileIndex: 1);
                }

                if (key == SDL.SDL_Keycode.SDLK_e)
                {
                    _mapEditor.SetBrush(tileIndex: 0);
                }
            }
        }

        public override void OnKeyUp(SDL.SDL_Keycode key)
        {
            if (key == SDL.SDL_Keycode.SDLK_LEFT) _isScrollingLeft = false;
            if (key == SDL.SDL_Keycode.SDLK_RIGHT) _isScrollingRight = false;
            if (key == SDL.SDL_Keycode.SDLK_UP) _isScrollingUp = false;
            if (key == SDL.SDL_Keycode.SDLK_DOWN) _isScrollingDown = false;
        }

        public override void OnMouseMove()
        {
            UpdateHoveredTile();

            if (_isDraggingScroll)
            {
                _scroll.X = _dragScroll.X - Desktop.MouseX / _zoom;
                _scroll.Y = _dragScroll.Y - Desktop.MouseY / _zoom;
            }
        }

        public override void OnMouseDown(int button)
        {
            if (_isDraggingScroll || _isPlacingTiles) return;

            Desktop.SetFocusedElement(this);
            Desktop.SetHoveredElementPressed(true);

            if (button == SDL.SDL_BUTTON_LEFT)
            {
                // TODO: Place a tile down or select an entity
                _isPlacingTiles = true;
                PutTile();
            }
            else if (button == SDL.SDL_BUTTON_MIDDLE)
            {
                _isScrollingLeft = false;
                _isScrollingRight = false;
                _isScrollingUp = false;
                _isScrollingDown = false;

                _isDraggingScroll = true;
                _dragScroll.X = _scroll.X + Desktop.MouseX / _zoom;
                _dragScroll.Y = _scroll.Y + Desktop.MouseY / _zoom;
            }
            else if (button == SDL.SDL_BUTTON_RIGHT)
            {
                _mapEditor.SetBrush(GetTileAt(_hoveredTile));
            }
        }

        public override void OnMouseUp(int button)
        {
            if (button == SDL.SDL_BUTTON_LEFT)
            {
                _isPlacingTiles = false;
                Desktop.SetHoveredElementPressed(false);
            }
            else if (button == SDL.SDL_BUTTON_MIDDLE && _isDraggingScroll)
            {
                _isDraggingScroll = false;
                Desktop.SetHoveredElementPressed(false);
            }
        }

        public override void OnMouseWheel(int dx, int dy)
        {
            if (!Desktop.IsShiftDown)
            {
                _zoom = Math.Clamp(_zoom + dy / 10f, MinZoom, MaxZoom);
            }
            else
            {
                _scroll.X += dx * 24 / _zoom;
                _scroll.Y -= dy * 24 / _zoom;
            }
        }

        void Animate(float deltaTime)
        {
            const float ScrollingSpeed = 800;
            var dx = 0;
            var dy = 0;

            if (_isScrollingLeft) dx--;
            if (_isScrollingRight) dx++;
            if (_isScrollingDown) dy--;
            if (_isScrollingUp) dy++;

            if (dx != 0 || dy != 0)
            {
                var angle = MathF.Atan2(dy, dx);
                _scroll.X += MathF.Cos(angle) * ScrollingSpeed * deltaTime / _zoom;
                _scroll.Y -= MathF.Sin(angle) * ScrollingSpeed * deltaTime / _zoom;
            }
        }
        #endregion

        protected override void DrawSelf()
        {
            base.DrawSelf();

            var viewportScroll = new Vector2(
                _scroll.X - ViewRectangle.Width / 2 / _zoom,
                _scroll.Y - ViewRectangle.Height / 2 / _zoom);

            var startTileCoords = new Point(
                (int)MathF.Floor(viewportScroll.X / Protocol.MapTileSize),
                (int)MathF.Floor(viewportScroll.Y / Protocol.MapTileSize));

            var endTileCoords = new Point(
                startTileCoords.X + (int)MathF.Ceiling(ViewRectangle.Width / (Protocol.MapTileSize * _zoom) + 1),
                startTileCoords.Y + (int)MathF.Ceiling(ViewRectangle.Height / (Protocol.MapTileSize * _zoom) + 1));

            Desktop.PushClipRect(ViewRectangle);

            new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);

            var startChunkCoords = new Point(
                (int)MathF.Floor((float)startTileCoords.X / Protocol.MapChunkSide),
                (int)MathF.Floor((float)startTileCoords.Y / Protocol.MapChunkSide));

            var endChunkCoords = new Point(
                (int)MathF.Floor((float)endTileCoords.X / Protocol.MapChunkSide),
                (int)MathF.Floor((float)endTileCoords.Y / Protocol.MapChunkSide));

            for (var chunkY = startChunkCoords.Y; chunkY <= endChunkCoords.Y; chunkY++)
            {
                for (var chunkX = startChunkCoords.X; chunkX <= endChunkCoords.X; chunkX++)
                {
                    if (!_chunks.TryGetValue(new Point(chunkX, chunkY), out var chunk)) continue;

                    var chunkStartTileCoords = new Point(chunkX * Protocol.MapChunkSide, chunkY * Protocol.MapChunkSide);

                    var chunkRelativeStartTileCoords = new Point(
                        Math.Max(0, startTileCoords.X - chunkStartTileCoords.X),
                        Math.Max(0, startTileCoords.Y - chunkStartTileCoords.Y));

                    var chunkRelativeEndTileCoords = new Point(
                        Math.Min(Protocol.MapChunkSide, endTileCoords.X - chunkStartTileCoords.X),
                        Math.Min(Protocol.MapChunkSide, endTileCoords.Y - chunkStartTileCoords.Y));

                    for (var chunkRelativeY = chunkRelativeStartTileCoords.Y; chunkRelativeY < chunkRelativeEndTileCoords.Y; chunkRelativeY++)
                    {
                        for (var chunkRelativeX = chunkRelativeStartTileCoords.X; chunkRelativeX < chunkRelativeEndTileCoords.X; chunkRelativeX++)
                        {
                            if (chunk.TilesPerLayer[0, chunkRelativeY * Protocol.MapChunkSide + chunkRelativeX] != 0)
                            {
                                var red = (byte)(255 * (chunkRelativeX - 16) / 32);
                                var blue = (byte)(255 * (chunkRelativeY - 16) / 32);
                                new Color((uint)((red << 24) + (blue << 8) + 0xff)).UseAsDrawColor(Desktop.Renderer);

                                var y = chunkStartTileCoords.Y + chunkRelativeY;
                                var x = chunkStartTileCoords.X + chunkRelativeX;

                                var left = ViewRectangle.X + (int)(x * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.X * _zoom);
                                var right = ViewRectangle.X + (int)((x + 1) * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.X * _zoom);
                                var top = ViewRectangle.Y + (int)(y * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.Y * _zoom);
                                var bottom = ViewRectangle.Y + (int)((y + 1) * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.Y * _zoom);

                                var destRect = new SDL.SDL_Rect { x = left, y = top, w = right - left, h = bottom - top };
                                SDL.SDL_RenderFillRect(Desktop.Renderer, ref destRect);
                            }
                        }
                    }
                }
            }

            if (IsHovered)
            {
                var color = new Color(0x00ff00ff);
                color.UseAsDrawColor(Desktop.Renderer);

                var x = _hoveredTile.X;
                var y = _hoveredTile.Y;
                var w = 1;
                var h = 1;

                var renderX = ViewRectangle.X + (int)(x * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.X * _zoom);
                var renderY = ViewRectangle.Y + (int)(y * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.Y * _zoom);

                var rect = new Rectangle(renderX, renderY, (int)(w * Protocol.MapTileSize * _zoom), (int)(h * Protocol.MapTileSize * _zoom));

                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X, rect.Y, rect.X + rect.Width, rect.Y);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X + rect.Width, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X + rect.Width, rect.Y + rect.Height, rect.X, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X, rect.Y + rect.Height, rect.X, rect.Y);
            }

            Desktop.PopClipRect();
        }

    }
}