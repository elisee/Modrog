﻿using DeepSwarmBasics;
using DeepSwarmBasics.Math;
using DeepSwarmClient.Graphics;
using DeepSwarmClient.UI;
using SDL2;
using System;
using System.Collections.Generic;

namespace DeepSwarmClient.Interface.Playing
{
    class PlayingView : InterfaceElement
    {
        public const int TileSize = 40;

        IntPtr SpritesheetTexture;

        // Scrolling
        // TODO: Allow support 2 levels of zoom or more idk
        public float ScrollingPixelsX { get; private set; }
        public float ScrollingPixelsY { get; private set; }

        bool _isScrollingLeft;
        bool _isScrollingRight;
        bool _isScrollingUp;
        bool _isScrollingDown;

        bool _isDraggingScroll;
        Point _dragScroll;

        // Hovered tile
        int _hoveredTileX;
        int _hoveredTileY;

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
            if (SpritesheetTexture != IntPtr.Zero)
            {
                SDL.SDL_DestroyTexture(SpritesheetTexture);
                SpritesheetTexture = IntPtr.Zero;
            }

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

            var state = Engine.State;

            if (state.SelectedEntity != null)
            {
                if (key == SDL.SDL_Keycode.SDLK_a || key == SDL.SDL_Keycode.SDLK_q) state.SetMoveTowards(DeepSwarmApi.EntityDirection.Left);
                if (key == SDL.SDL_Keycode.SDLK_d) state.SetMoveTowards(DeepSwarmApi.EntityDirection.Right);
                if (key == SDL.SDL_Keycode.SDLK_w || key == SDL.SDL_Keycode.SDLK_z) state.SetMoveTowards(DeepSwarmApi.EntityDirection.Up);
                if (key == SDL.SDL_Keycode.SDLK_s) state.SetMoveTowards(DeepSwarmApi.EntityDirection.Down);
            }
        }

        public override void OnKeyUp(SDL.SDL_Keycode key)
        {
            if (key == SDL.SDL_Keycode.SDLK_LEFT) _isScrollingLeft = false;
            if (key == SDL.SDL_Keycode.SDLK_RIGHT) _isScrollingRight = false;
            if (key == SDL.SDL_Keycode.SDLK_UP) _isScrollingUp = false;
            if (key == SDL.SDL_Keycode.SDLK_DOWN) _isScrollingDown = false;

            if (key == SDL.SDL_Keycode.SDLK_a || key == SDL.SDL_Keycode.SDLK_q) Engine.State.StopMovingTowards(DeepSwarmApi.EntityDirection.Left);
            if (key == SDL.SDL_Keycode.SDLK_d) Engine.State.StopMovingTowards(DeepSwarmApi.EntityDirection.Right);
            if (key == SDL.SDL_Keycode.SDLK_w || key == SDL.SDL_Keycode.SDLK_z) Engine.State.StopMovingTowards(DeepSwarmApi.EntityDirection.Up);
            if (key == SDL.SDL_Keycode.SDLK_s) Engine.State.StopMovingTowards(DeepSwarmApi.EntityDirection.Down);
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
            if (button == 1)
            {
                var startTileX = (int)ScrollingPixelsX / TileSize;
                var startTileY = (int)ScrollingPixelsY / TileSize;

                var hoveredEntities = new List<Game.ClientEntity>();

                foreach (var entity in Engine.State.SeenEntities)
                {
                    if (entity.Position.X == _hoveredTileX && entity.Position.Y == _hoveredTileY) hoveredEntities.Add(entity);
                }

                if (hoveredEntities.Count > 0)
                {
                    if (Engine.State.SelectedEntity == null || hoveredEntities.Count == 1)
                    {
                        Engine.State.SelectEntity(hoveredEntities[0]);
                    }
                    else
                    {
                        var selectedEntityIndex = hoveredEntities.IndexOf(Engine.State.SelectedEntity);
                        var newSelectedEntityIndex = selectedEntityIndex < hoveredEntities.Count - 1 ? selectedEntityIndex + 1 : 0;
                        Engine.State.SelectEntity(hoveredEntities[newSelectedEntityIndex]);
                    }
                }
                else Engine.State.SelectEntity(null);
            }
            else if (button == 2)
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

        public void OnSpritesheetReceived(Span<byte> data)
        {
            if (SpritesheetTexture != IntPtr.Zero) SDL.SDL_DestroyTexture(SpritesheetTexture);

            unsafe
            {
                fixed (byte* dataPointer = data)
                {
                    var rwOps = SDL.SDL_RWFromMem((IntPtr)dataPointer, data.Length);
                    SpritesheetTexture = SDL_image.IMG_LoadTexture_RW(Engine.Renderer, rwOps, freesrc: 1);
                }
            }
        }

        public void OnTeleported(Point position)
        {
            ScrollingPixelsX = position.X * TileSize;
            ScrollingPixelsY = position.Y * TileSize;
        }

        public void OnSelectedEntityChanged()
        {
            // TODO
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

            var viewportScrollX = -Engine.Interface.Viewport.Width / 2 + (int)ScrollingPixelsX;
            var viewportScrollY = -Engine.Interface.Viewport.Height / 2 + (int)ScrollingPixelsY;

            _hoveredTileX = ((int)viewportScrollX + Desktop.MouseX) / TileSize;
            _hoveredTileY = ((int)viewportScrollY + Desktop.MouseY) / TileSize;
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
                        var sourceRect = new SDL.SDL_Rect { x = tile * TileSize, y = 0, w = TileSize, h = TileSize };
                        var destRect = new SDL.SDL_Rect { x = x * TileSize - viewportScrollX, y = y * TileSize - viewportScrollY, w = TileSize, h = TileSize };
                        SDL.SDL_RenderCopy(Engine.Renderer, SpritesheetTexture, ref sourceRect, ref destRect);
                    }
                }
            }

            foreach (var entity in state.SeenEntities)
            {
                if (entity.Position.X < startTileX || entity.Position.Y < startTileY || entity.Position.X > endTileX || entity.Position.Y > endTileY) continue;

                var rect = new SDL.SDL_Rect { x = entity.Position.X * TileSize - viewportScrollX, y = entity.Position.Y * TileSize - viewportScrollY, w = TileSize, h = TileSize };
                new Color(0x008800ff).UseAsDrawColor(Engine.Renderer);
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

            if (state.SelectedEntity != null)
            {
                var color = new Color(0x00ff00ff);
                color.UseAsDrawColor(Engine.Renderer);

                var x = state.SelectedEntity.Position.X;
                var y = state.SelectedEntity.Position.Y;
                var w = 1;
                var h = 1;

                var renderX = x * TileSize - viewportScrollX;
                var renderY = y * TileSize - viewportScrollY;

                var rect = new Rectangle(renderX, renderY, w * TileSize, h * TileSize);

                SDL.SDL_RenderDrawLine(Engine.Renderer, rect.X, rect.Y, rect.X + rect.Width, rect.Y);
                SDL.SDL_RenderDrawLine(Engine.Renderer, rect.X + rect.Width, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Engine.Renderer, rect.X + rect.Width, rect.Y + rect.Height, rect.X, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Engine.Renderer, rect.X, rect.Y + rect.Height, rect.X, rect.Y);
            }
        }
    }
}
