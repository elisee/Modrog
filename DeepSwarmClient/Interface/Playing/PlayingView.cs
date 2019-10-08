using DeepSwarmBasics;
using DeepSwarmBasics.Math;
using DeepSwarmClient.Graphics;
using DeepSwarmClient.UI;
using SDL2;
using System;

namespace DeepSwarmClient.Interface.Playing
{
    class PlayingView : InterfaceElement
    {
        public const int TileSize = 24;

        // TODO: Allow support 2 levels of zoom or more idk
        public float ScrollingPixelsX { get; private set; }
        public float ScrollingPixelsY { get; private set; }

        bool _isScrollingLeft;
        bool _isScrollingRight;
        bool _isScrollingUp;
        bool _isScrollingDown;

        bool _isDraggingScroll;
        Point _dragScroll;

        public PlayingView(Interface @interface)
            : base(@interface, null)
        {
        }

        public override Element HitTest(int x, int y)
        {
            return base.HitTest(x, y) ?? (LayoutRectangle.Contains(x, y) ? this : null);
        }

        public override void OnMounted()
        {
            Desktop.RegisterAnimation(Animate);
            Desktop.SetFocusedElement(this);
        }

        public override void OnUnmounted()
        {
            Desktop.UnregisterAnimation(Animate);
        }

        public override void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            if (repeat) return;

            if (!_isDraggingScroll)
            {
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
                ScrollingPixelsX = _dragScroll.X - Desktop.MouseX;
                ScrollingPixelsY = _dragScroll.Y - Desktop.MouseY;
            }
        }

        public override void OnMouseDown(int button)
        {
            if (button == 2)
            {
                _isScrollingLeft = false;
                _isScrollingRight = false;
                _isScrollingUp = false;
                _isScrollingDown = false;

                _isDraggingScroll = true;
                _dragScroll.X = (int)ScrollingPixelsX + Desktop.MouseX;
                _dragScroll.Y = (int)ScrollingPixelsY + Desktop.MouseY;
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
            ScrollingPixelsX += dx * 12;
            ScrollingPixelsY -= dy * 12;
        }

        public void OnPlayerListUpdated()
        {
        }

        public void OnChatMessageReceived(string author, string message)
        {
        }

        public void OnTeleported(Point position)
        {
            ScrollingPixelsX = position.X * TileSize;
            ScrollingPixelsY = position.Y * TileSize;
        }

        public void Animate(float deltaTime)
        {
            const float ScrollingSpeed = 400;
            var dx = 0;
            var dy = 0;

            if (_isScrollingLeft) dx--;
            if (_isScrollingRight) dx++;
            if (_isScrollingDown) dy--;
            if (_isScrollingUp) dy++;

            if (dx != 0 || dy != 0)
            {
                var angle = MathF.Atan2(dy, dx);
                ScrollingPixelsX += MathF.Cos(angle) * ScrollingSpeed * deltaTime;
                ScrollingPixelsY -= MathF.Sin(angle) * ScrollingSpeed * deltaTime;
            }
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            var state = Engine.State;
            if (state.WorldSize.X == 0 || state.WorldSize.Y == 0) return;

            var viewportScrollX = -Engine.Interface.Viewport.Width / 2 + (int)ScrollingPixelsX;
            var viewportScrollY = -Engine.Interface.Viewport.Height / 2 + (int)ScrollingPixelsY;

            var startTileX = Math.Max(0, (int)viewportScrollX / TileSize);
            var startTileY = Math.Max(0, (int)viewportScrollY / TileSize);

            var endTileX = Math.Min(state.WorldSize.X - 1, startTileX + (int)MathF.Ceiling((float)Engine.Interface.Viewport.Width / TileSize + 1));
            var endTileY = Math.Min(state.WorldSize.Y - 1, startTileY + (int)MathF.Ceiling((float)Engine.Interface.Viewport.Height / TileSize + 1));

            new Color(0xffffffff).UseAsDrawColor(Engine.Renderer);

            for (var y = startTileY; y <= endTileY; y++)
            {
                for (var x = startTileX; x <= endTileX; x++)
                {
                    var tile = state.WorldTiles[y * state.WorldSize.X + x];

                    if (tile != 0)
                    {
                        new Color(0x880000ff).UseAsDrawColor(Engine.Renderer);
                        var rect = new SDL.SDL_Rect { x = x * TileSize - viewportScrollX, y = y * TileSize - viewportScrollY, w = TileSize, h = TileSize };
                        SDL.SDL_RenderFillRect(Engine.Renderer, ref rect);
                    }
                }
            }

            foreach (var entity in state.SeenEntities)
            {
                if (entity.Position.X < startTileX || entity.Position.Y < startTileY || entity.Position.X > endTileX || entity.Position.Y > endTileY) continue;

                var rect = new SDL.SDL_Rect { x = entity.Position.X * TileSize - viewportScrollX, y = entity.Position.Y * TileSize - viewportScrollY, w = TileSize, h = TileSize };
                new Color(0x00ff00ff).UseAsDrawColor(Engine.Renderer);
                SDL.SDL_RenderFillRect(Engine.Renderer, ref rect);
            }

            var fog = Engine.State.WorldFog;
            var fogColor = new Color(0x00000044);
            fogColor.UseAsDrawColor(Engine.Renderer);
            SDL.SDL_SetRenderDrawBlendMode(Engine.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

            for (var y = startTileY; y <= endTileY; y++)
            {
                for (var x = startTileX; x <= endTileX; x++)
                {
                    var tileIndex = y * state.WorldSize.X + x;
                    if (fog[tileIndex] != 0) continue;

                    var rect = new SDL.SDL_Rect { x = x * TileSize - viewportScrollX, y = y * TileSize - viewportScrollY, w = TileSize, h = TileSize };
                    SDL.SDL_RenderFillRect(Engine.Renderer, ref rect);
                }
            }

            SDL.SDL_SetRenderDrawBlendMode(Engine.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);
        }
    }
}
