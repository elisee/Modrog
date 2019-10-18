using DeepSwarmBasics;
using DeepSwarmBasics.Math;
using DeepSwarmCommon;
using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.UI;
using SDL2;
using System;
using System.Collections.Generic;

namespace DeepSwarmScenarioEditor.Interface.Editing.Map
{
    class MapViewport : Panel
    {
        class Chunk
        {
            public readonly short[,] TilesPerLayer;
            public Chunk() { TilesPerLayer = new short[(int)Protocol.MapLayer.Count, Protocol.MapChunkSide * Protocol.MapChunkSide]; }
        }

        readonly Dictionary<Point, Chunk> _chunks = new Dictionary<Point, Chunk>();

        // Scrolling
        // TODO: Allow support 2 levels of zoom or more idk
        float _scrollingPixelsX;
        float _scrollingPixelsY;
        float _zoom = 2f;

        bool _isScrollingLeft;
        bool _isScrollingRight;
        bool _isScrollingUp;
        bool _isScrollingDown;

        bool _isDraggingScroll;

        Point _dragScroll;

        // Hovered tile
        int _hoveredTileX;
        int _hoveredTileY;

        public MapViewport(MapEditor mapEditor) : base(mapEditor.Desktop, null)
        {
        }

        #region Internals
        public override bool AcceptsFocus() => true;

        public override Element HitTest(int x, int y)
        {
            return base.HitTest(x, y) ?? (ViewRectangle.Contains(x, y) ? this : null);
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
            if (repeat) return;

            if (!_isDraggingScroll)
            {
                if (key == SDL.SDL_Keycode.SDLK_KP_PLUS) _zoom = Math.Min(_zoom + 0.1f, 2f);
                if (key == SDL.SDL_Keycode.SDLK_KP_MINUS) _zoom = Math.Max(_zoom - 0.1f, 0.5f);

                if (key == SDL.SDL_Keycode.SDLK_LEFT) _isScrollingLeft = true;
                if (key == SDL.SDL_Keycode.SDLK_RIGHT) _isScrollingRight = true;
                if (key == SDL.SDL_Keycode.SDLK_UP) _isScrollingUp = true;
                if (key == SDL.SDL_Keycode.SDLK_DOWN) _isScrollingDown = true;
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
            if (_isDraggingScroll)
            {
                _scrollingPixelsX = _dragScroll.X - Desktop.MouseX;
                _scrollingPixelsY = _dragScroll.Y - Desktop.MouseY;
            }
        }

        public override void OnMouseDown(int button)
        {
            if (button == 1)
            {
                var startTileX = (int)_scrollingPixelsX / Protocol.MapTileSize;
                var startTileY = (int)_scrollingPixelsY / Protocol.MapTileSize;

                // TODO: Entity selection
            }
            else if (button == 2)
            {
                _isScrollingLeft = false;
                _isScrollingRight = false;
                _isScrollingUp = false;
                _isScrollingDown = false;

                _isDraggingScroll = true;
                _dragScroll.X = (int)_scrollingPixelsX + Desktop.MouseX;
                _dragScroll.Y = (int)_scrollingPixelsY + Desktop.MouseY;
            }
        }

        public override void OnMouseUp(int button)
        {
            if (button == 2)
            {
                _isDraggingScroll = false;
            }
        }

        public override void OnMouseWheel(int dx, int dy)
        {
            if (Desktop.IsCtrlDown)
            {
                _zoom = Math.Clamp(_zoom + dy / 10f, 0.5f, 2f);
            }
            else
            {
                _scrollingPixelsX += dx * 24 / _zoom;
                _scrollingPixelsY -= dy * 24 / _zoom;
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
                _scrollingPixelsX += MathF.Cos(angle) * ScrollingSpeed * deltaTime / _zoom;
                _scrollingPixelsY -= MathF.Sin(angle) * ScrollingSpeed * deltaTime / _zoom;
            }

            var viewportScrollX = (int)_scrollingPixelsX - (int)(ViewRectangle.Width / 2 / _zoom);
            var viewportScrollY = (int)_scrollingPixelsY - (int)(ViewRectangle.Height / 2 / _zoom);

            _hoveredTileX = (int)MathF.Floor((float)(viewportScrollX + (Desktop.MouseX - ViewRectangle.X) / _zoom) / Protocol.MapTileSize);
            _hoveredTileY = (int)MathF.Floor((float)(viewportScrollY + (Desktop.MouseY - ViewRectangle.Y) / _zoom) / Protocol.MapTileSize);
        }
        #endregion

        protected override void DrawSelf()
        {
            base.DrawSelf();

            var viewportScrollX = (int)_scrollingPixelsX - (int)(ViewRectangle.Width / 2 / _zoom);
            var viewportScrollY = (int)_scrollingPixelsY - (int)(ViewRectangle.Height / 2 / _zoom);

            var startTileX = (int)MathF.Floor((float)viewportScrollX / Protocol.MapTileSize);
            var startTileY = (int)MathF.Floor((float)viewportScrollY / Protocol.MapTileSize);

            var endTileX = startTileX + (int)MathF.Ceiling((float)ViewRectangle.Width / (Protocol.MapTileSize * _zoom) + 1);
            var endTileY = startTileY + (int)MathF.Ceiling((float)ViewRectangle.Height / (Protocol.MapTileSize * _zoom) + 1);

            Desktop.PushClipRect(ViewRectangle);

            new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);

            for (var y = startTileY; y <= endTileY; y++)
            {
                for (var x = startTileX; x <= endTileX; x++)
                {
                    var red = (byte)(255 * (x - 16) / 32);
                    var blue = (byte)(255 * (y - 16) / 32);
                    new Color((uint)((red << 24) + (blue << 8) + 0xff)).UseAsDrawColor(Desktop.Renderer);

                    var left = ViewRectangle.X + (int)(x * _zoom * Protocol.MapTileSize) - (int)(viewportScrollX * _zoom);
                    var right = ViewRectangle.X + (int)((x + 1) * _zoom * Protocol.MapTileSize) - (int)(viewportScrollX * _zoom);
                    var top = ViewRectangle.Y + (int)(y * _zoom * Protocol.MapTileSize) - (int)(viewportScrollY * _zoom);
                    var bottom = ViewRectangle.Y + (int)((y + 1) * _zoom * Protocol.MapTileSize) - (int)(viewportScrollY * _zoom);

                    var destRect = new SDL.SDL_Rect { x = left, y = top, w = right - left, h = bottom - top };
                    SDL.SDL_RenderFillRect(Desktop.Renderer, ref destRect);

                    /*
                    var tile = state.WorldTiles[y * state.WorldSize.X + x];

                    if (tile != 0)
                    {
                        var spriteLocation = state.TileKinds[tile].SpriteLocation;
                        var sourceRect = new SDL.SDL_Rect { x = spriteLocation.X * TileSize, y = spriteLocation.Y * TileSize, w = TileSize, h = TileSize };
                        SDL.SDL_RenderCopy(Engine.Renderer, SpritesheetTexture, ref sourceRect, ref destRect);
                    }
                    */
                }
            }

            {
                var color = new Color(0x00ff00ff);
                color.UseAsDrawColor(Desktop.Renderer);

                var x = _hoveredTileX;
                var y = _hoveredTileY;
                var w = 1;
                var h = 1;

                var renderX = ViewRectangle.X + (int)(x * _zoom * Protocol.MapTileSize) - (int)(viewportScrollX * _zoom);
                var renderY = ViewRectangle.Y + (int)(y * _zoom * Protocol.MapTileSize) - (int)(viewportScrollY * _zoom);

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
