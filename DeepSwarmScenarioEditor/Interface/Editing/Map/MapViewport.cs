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
        Vector2 _scroll;
        float _zoom = 2f;

        bool _isScrollingLeft;
        bool _isScrollingRight;
        bool _isScrollingUp;
        bool _isScrollingDown;

        bool _isDraggingScroll;
        Vector2 _dragScroll;

        // Hovered tile
        Point _hoveredTile;

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
                _scroll.X = _dragScroll.X - Desktop.MouseX / _zoom;
                _scroll.Y = _dragScroll.Y - Desktop.MouseY / _zoom;
            }
        }

        public override void OnMouseDown(int button)
        {
            if (button == 1)
            {
                Desktop.SetFocusedElement(this);
                Desktop.SetHoveredElementPressed(true);

                // TODO: Entity selection
            }
            else if (button == 2)
            {
                Desktop.SetFocusedElement(this);
                Desktop.SetHoveredElementPressed(true);
                _isScrollingLeft = false;
                _isScrollingRight = false;
                _isScrollingUp = false;
                _isScrollingDown = false;

                _isDraggingScroll = true;
                _dragScroll.X = _scroll.X + Desktop.MouseX / _zoom;
                _dragScroll.Y = _scroll.Y + Desktop.MouseY / _zoom;
            }
        }

        public override void OnMouseUp(int button)
        {
            if (button == 1)
            {
                Desktop.SetHoveredElementPressed(false);
            }
            else if (button == 2 && _isDraggingScroll)
            {
                _isDraggingScroll = false;
                Desktop.SetHoveredElementPressed(false);
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

            var viewportScroll = new Vector2(
                _scroll.X - ViewRectangle.Width / 2 / _zoom,
                _scroll.Y - ViewRectangle.Height / 2 / _zoom);

            _hoveredTile = new Point(
                (int)MathF.Floor((viewportScroll.X + (Desktop.MouseX - ViewRectangle.X) / _zoom) / Protocol.MapTileSize),
                (int)MathF.Floor((viewportScroll.Y + (Desktop.MouseY - ViewRectangle.Y) / _zoom) / Protocol.MapTileSize));
        }
        #endregion

        protected override void DrawSelf()
        {
            base.DrawSelf();

            var viewportScroll = new Vector2(
                _scroll.X - ViewRectangle.Width / 2 / _zoom,
                _scroll.Y - ViewRectangle.Height / 2 / _zoom);

            var startTile = new Point(
                (int)MathF.Floor(viewportScroll.X / Protocol.MapTileSize),
                (int)MathF.Floor(viewportScroll.Y / Protocol.MapTileSize));

            var endTile = new Point(
                startTile.X + (int)MathF.Ceiling(ViewRectangle.Width / (Protocol.MapTileSize * _zoom) + 1),
                startTile.Y + (int)MathF.Ceiling(ViewRectangle.Height / (Protocol.MapTileSize * _zoom) + 1));

            Desktop.PushClipRect(ViewRectangle);

            new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);

            for (var y = startTile.Y; y <= endTile.Y; y++)
            {
                for (var x = startTile.X; x <= endTile.X; x++)
                {
                    var red = (byte)(255 * (x - 16) / 32);
                    var blue = (byte)(255 * (y - 16) / 32);
                    new Color((uint)((red << 24) + (blue << 8) + 0xff)).UseAsDrawColor(Desktop.Renderer);

                    var left = ViewRectangle.X + (int)(x * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.X * _zoom);
                    var right = ViewRectangle.X + (int)((x + 1) * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.X * _zoom);
                    var top = ViewRectangle.Y + (int)(y * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.Y * _zoom);
                    var bottom = ViewRectangle.Y + (int)((y + 1) * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.Y * _zoom);

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
