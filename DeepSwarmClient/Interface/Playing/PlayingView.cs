﻿using DeepSwarmBasics;
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
        float _scrollingPixelsX;
        float _scrollingPixelsY;

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
                _scrollingPixelsX = _dragScroll.X - Desktop.MouseX;
                _scrollingPixelsY = _dragScroll.Y - Desktop.MouseY;
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
            _scrollingPixelsX += dx * 12;
            _scrollingPixelsY -= dy * 12;
        }

        public void OnPlayerListUpdated()
        {
        }

        public void OnChatMessageReceived(string author, string message)
        {
        }

        public void OnTeleported(Point position)
        {
            _scrollingPixelsX = position.X * TileSize;
            _scrollingPixelsY = position.Y * TileSize;
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
                _scrollingPixelsX += MathF.Cos(angle) * ScrollingSpeed * deltaTime;
                _scrollingPixelsY -= MathF.Sin(angle) * ScrollingSpeed * deltaTime;
            }
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            var state = Engine.State;

            var viewportScrollX = -Engine.Interface.Viewport.Width / 2 + (int)_scrollingPixelsX;
            var viewportScrollY = -Engine.Interface.Viewport.Height / 2 + (int)_scrollingPixelsY;

            new Color(0xffffffff).UseAsDrawColor(Engine.Renderer);

            for (var j = 0; j < state.WorldSize.Y; j++)
            {
                for (var i = 0; i < state.WorldSize.X; i++)
                {
                    // Desktop.MonoFontStyle.DrawText(i * TileSize - viewportScrollX, j * TileSize - viewportScrollY, $"{i},{j}");

                    var tile = state.WorldTiles[j * state.WorldSize.X + i];
                    if (tile != 0)
                    {
                        new Color(0x880000ff).UseAsDrawColor(Engine.Renderer);
                        var rect = new SDL.SDL_Rect { x = i * TileSize - viewportScrollX, y = j * TileSize - viewportScrollY, w = TileSize, h = TileSize };
                        SDL.SDL_RenderFillRect(Engine.Renderer, ref rect);

                        new Color(0xffffffff).UseAsDrawColor(Engine.Renderer);
                    }
                }
            }
        }
    }
}
