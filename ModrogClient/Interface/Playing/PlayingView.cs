using ModrogCommon;
using SDL2;
using SwarmBasics;
using SwarmBasics.Math;
using SwarmPlatform.Graphics;
using SwarmPlatform.UI;
using System;
using System.Collections.Generic;

namespace ModrogClient.Interface.Playing
{
    class PlayingView : ClientElement
    {
        IntPtr SpritesheetTexture;

        // Scrolling
        public Vector2 Scroll { get; private set; }
        float _zoom = 2f;
        const float MinZoom = 0.5f;
        const float MaxZoom = 2f;

        bool _isScrollingLeft;
        bool _isScrollingRight;
        bool _isScrollingUp;
        bool _isScrollingDown;

        bool _isDraggingScroll;
        Vector2 _dragScroll;

        // Hovered tile
        Point _hoveredTileCoords;

        // Player list
        readonly Element _sidebarPanel;
        readonly Label _serverNameLabel;
        readonly Label _scenarioNameLabel;
        readonly Panel _playerListContainer;

        // Menu
        readonly PlayingMenu _menu;

        public PlayingView(ClientApp app)
            : base(app, null)
        {
            _sidebarPanel = new Panel(Desktop, this)
            {
                Visible = false,
                BackgroundPatch = new TexturePatch(0x123456ff),
                Left = 0,
                Width = 300,
                ChildLayout = ChildLayoutMode.Top,
            };

            _serverNameLabel = new Label(_sidebarPanel) { Ellipsize = true, Padding = 8, BackgroundPatch = new TexturePatch(0x113311ff) };
            _scenarioNameLabel = new Label(_sidebarPanel) { FontStyle = app.HeaderFontStyle, Wrap = true, Padding = 8, BackgroundPatch = new TexturePatch(0x331111ff) };
            _playerListContainer = new Panel(_sidebarPanel) { LayoutWeight = 1, ChildLayout = ChildLayoutMode.Top, Padding = 8 };

            _menu = new PlayingMenu(app, this) { Visible = false };
        }

        #region Internals
        void UpdateHoveredTile()
        {
            var viewportScroll = new Vector2(
                Scroll.X - ViewRectangle.Width / 2 / _zoom,
                Scroll.Y - ViewRectangle.Height / 2 / _zoom);

            var newHoveredTileCoords = new Point(
                (int)MathF.Floor((viewportScroll.X + (Desktop.MouseX - ViewRectangle.X) / _zoom) / Protocol.MapTileSize),
                (int)MathF.Floor((viewportScroll.Y + (Desktop.MouseY - ViewRectangle.Y) / _zoom) / Protocol.MapTileSize));

            var hasHoveredTileChanged = _hoveredTileCoords != newHoveredTileCoords;
            _hoveredTileCoords = newHoveredTileCoords;
        }

        public override Element HitTest(int x, int y)
        {
            return base.HitTest(x, y) ?? (LayoutRectangle.Contains(x, y) ? this : null);
        }

        public override void OnMounted()
        {
            Desktop.RegisterAnimation(Animate);
            Desktop.SetFocusedElement(this);

            _serverNameLabel.Text = $"{App.State.SavedServerHostname}:{App.State.SavedServerPort}";
            _scenarioNameLabel.Text = App.State.ActiveScenario.Title;

            OnPlayerListUpdated();
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
                if (key == SDL.SDL_Keycode.SDLK_ESCAPE) App.State.SetPlayingMenuOpen(true);

                if (key == SDL.SDL_Keycode.SDLK_LEFT) _isScrollingLeft = true;
                if (key == SDL.SDL_Keycode.SDLK_RIGHT) _isScrollingRight = true;
                if (key == SDL.SDL_Keycode.SDLK_UP) _isScrollingUp = true;
                if (key == SDL.SDL_Keycode.SDLK_DOWN) _isScrollingDown = true;
            }

            var state = App.State;

            if (state.SelectedEntity != null)
            {
                if (key == SDL.SDL_Keycode.SDLK_a || key == SDL.SDL_Keycode.SDLK_q) state.SetMoveTowards(ModrogApi.EntityDirection.Left);
                if (key == SDL.SDL_Keycode.SDLK_d) state.SetMoveTowards(ModrogApi.EntityDirection.Right);
                if (key == SDL.SDL_Keycode.SDLK_w || key == SDL.SDL_Keycode.SDLK_z) state.SetMoveTowards(ModrogApi.EntityDirection.Up);
                if (key == SDL.SDL_Keycode.SDLK_s) state.SetMoveTowards(ModrogApi.EntityDirection.Down);
            }

            if (key == SDL.SDL_Keycode.SDLK_TAB)
            {
                _sidebarPanel.Visible = true;
                _sidebarPanel.Layout(_contentRectangle);
            }
        }

        public override void OnKeyUp(SDL.SDL_Keycode key)
        {
            if (key == SDL.SDL_Keycode.SDLK_LEFT) _isScrollingLeft = false;
            if (key == SDL.SDL_Keycode.SDLK_RIGHT) _isScrollingRight = false;
            if (key == SDL.SDL_Keycode.SDLK_UP) _isScrollingUp = false;
            if (key == SDL.SDL_Keycode.SDLK_DOWN) _isScrollingDown = false;

            if (key == SDL.SDL_Keycode.SDLK_a || key == SDL.SDL_Keycode.SDLK_q) App.State.StopMovingTowards(ModrogApi.EntityDirection.Left);
            if (key == SDL.SDL_Keycode.SDLK_d) App.State.StopMovingTowards(ModrogApi.EntityDirection.Right);
            if (key == SDL.SDL_Keycode.SDLK_w || key == SDL.SDL_Keycode.SDLK_z) App.State.StopMovingTowards(ModrogApi.EntityDirection.Up);
            if (key == SDL.SDL_Keycode.SDLK_s) App.State.StopMovingTowards(ModrogApi.EntityDirection.Down);

            if (key == SDL.SDL_Keycode.SDLK_TAB) _sidebarPanel.Visible = false;
        }

        public override void OnMouseMove()
        {
            UpdateHoveredTile();

            if (_isDraggingScroll)
            {
                Scroll = new Vector2(
                    _dragScroll.X - Desktop.MouseX / _zoom,
                    _dragScroll.Y - Desktop.MouseY / _zoom);
            }
        }

        public override void OnMouseDown(int button, int clicks)
        {
            if (button == SDL.SDL_BUTTON_LEFT)
            {
                var hoveredEntities = new List<Game.ClientEntity>();

                foreach (var entity in App.State.SeenEntities)
                {
                    if (entity.Position.X == _hoveredTileCoords.X && entity.Position.Y == _hoveredTileCoords.Y) hoveredEntities.Add(entity);
                }

                if (hoveredEntities.Count > 0)
                {
                    if (App.State.SelectedEntity == null || hoveredEntities.Count == 1)
                    {
                        App.State.SelectEntity(hoveredEntities[0]);
                    }
                    else
                    {
                        var selectedEntityIndex = hoveredEntities.IndexOf(App.State.SelectedEntity);
                        var newSelectedEntityIndex = selectedEntityIndex < hoveredEntities.Count - 1 ? selectedEntityIndex + 1 : 0;
                        App.State.SelectEntity(hoveredEntities[newSelectedEntityIndex]);
                    }
                }
                else App.State.SelectEntity(null);
            }
            else if (button == SDL.SDL_BUTTON_MIDDLE)
            {
                _isScrollingLeft = false;
                _isScrollingRight = false;
                _isScrollingUp = false;
                _isScrollingDown = false;

                _isDraggingScroll = true;
                _dragScroll = new Vector2(Scroll.X + Desktop.MouseX / _zoom, Scroll.Y + Desktop.MouseY / _zoom);
            }
        }

        public override void OnMouseUp(int button)
        {
            if (button == SDL.SDL_BUTTON_MIDDLE && _isDraggingScroll)
            {
                _isDraggingScroll = false;
            }
        }

        public override void OnMouseWheel(int dx, int dy)
        {
            if (Desktop.HasNoKeyModifier || Desktop.HasShiftKeyModifierAlone)
            {
                if (!Desktop.HasShiftKeyModifier) _zoom = Math.Clamp(_zoom + dy / 10f, MinZoom, MaxZoom);
                else Scroll = new Vector2(Scroll.X + dx * 24 / _zoom, Scroll.Y - dy * 24 / _zoom);
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
                Scroll = new Vector2(
                    Scroll.X + MathF.Cos(angle) * ScrollingSpeed * deltaTime / _zoom,
                    Scroll.Y - MathF.Sin(angle) * ScrollingSpeed * deltaTime / _zoom);
            }
        }
        #endregion

        #region Events
        public void OnMenuStateUpdated()
        {
            _menu.Visible = App.State.PlayingMenuOpen;
            _menu.Layout(_contentRectangle);

            if (!App.State.PlayingMenuOpen) Desktop.SetFocusedElement(this);
        }

        public void OnPlayerListUpdated()
        {
            _playerListContainer.Clear();

            foreach (var player in App.State.PlayerList)
            {
                var panel = new Panel(_playerListContainer) { ChildLayout = ChildLayoutMode.Left };

                new Element(panel) { Width = 12, Height = 12, Right = 8, BackgroundPatch = new TexturePatch(player.IsOnline ? 0x44ff44ff : 0xff4444ff) };

                // TODO: Icon for host/not host

                new Label(panel) { LayoutWeight = 1, Text = player.Name };
            }

            _playerListContainer.Layout();
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
                    SpritesheetTexture = SDL_image.IMG_LoadTexture_RW(Desktop.Renderer, rwOps, freesrc: 1);
                }
            }
        }

        public void OnTeleported(Point position)
        {
            Scroll = new Vector2(position.X * Protocol.MapTileSize, position.Y * Protocol.MapTileSize);
        }

        public void OnSelectedEntityChanged()
        {
            // TODO
        }
        #endregion

        #region Drawing
        protected override void DrawSelf()
        {
            base.DrawSelf();

            var state = App.State;
            if (state.WorldChunks.Count == 0) return;

            var viewportScroll = new Vector2(
                Scroll.X - ViewRectangle.Width / 2 / _zoom,
                Scroll.Y - ViewRectangle.Height / 2 / _zoom);

            var startTileCoords = new Point(
                (int)MathF.Floor(viewportScroll.X / Protocol.MapTileSize),
                (int)MathF.Floor(viewportScroll.Y / Protocol.MapTileSize));

            var endTileCoords = new Point(
                startTileCoords.X + (int)MathF.Ceiling(ViewRectangle.Width / (Protocol.MapTileSize * _zoom) + 1),
                startTileCoords.Y + (int)MathF.Ceiling(ViewRectangle.Height / (Protocol.MapTileSize * _zoom) + 1));

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
                    if (!state.WorldChunks.TryGetValue(new Point(chunkX, chunkY), out var chunk)) continue;

                    var chunkStartTileCoords = new Point(chunkX * Protocol.MapChunkSide, chunkY * Protocol.MapChunkSide);

                    var chunkRelativeStartTileCoords = new Point(
                        Math.Max(0, startTileCoords.X - chunkStartTileCoords.X),
                        Math.Max(0, startTileCoords.Y - chunkStartTileCoords.Y));

                    var chunkRelativeEndTileCoords = new Point(
                        Math.Min(Protocol.MapChunkSide, endTileCoords.X - chunkStartTileCoords.X),
                        Math.Min(Protocol.MapChunkSide, endTileCoords.Y - chunkStartTileCoords.Y));

                    for (var tileLayer = 0; tileLayer < (int)ModrogApi.MapLayer.Count; tileLayer++)
                    {
                        var tileKinds = state.TileKindsByLayer[tileLayer];

                        for (var chunkRelativeY = chunkRelativeStartTileCoords.Y; chunkRelativeY < chunkRelativeEndTileCoords.Y; chunkRelativeY++)
                        {
                            for (var chunkRelativeX = chunkRelativeStartTileCoords.X; chunkRelativeX < chunkRelativeEndTileCoords.X; chunkRelativeX++)
                            {
                                var tile = chunk.TilesPerLayer[tileLayer][chunkRelativeY * Protocol.MapChunkSide + chunkRelativeX];
                                if (tile == 0) continue;

                                var y = chunkStartTileCoords.Y + chunkRelativeY;
                                var x = chunkStartTileCoords.X + chunkRelativeX;

                                var left = ViewRectangle.X + (int)(x * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.X * _zoom);
                                var right = ViewRectangle.X + (int)((x + 1) * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.X * _zoom);
                                var top = ViewRectangle.Y + (int)(y * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.Y * _zoom);
                                var bottom = ViewRectangle.Y + (int)((y + 1) * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.Y * _zoom);

                                var spriteLocation = tileKinds[tile - 1].SpriteLocation;
                                var sourceRect = new SDL.SDL_Rect { x = spriteLocation.X * Protocol.MapTileSize, y = spriteLocation.Y * Protocol.MapTileSize, w = Protocol.MapTileSize, h = Protocol.MapTileSize };
                                var destRect = new SDL.SDL_Rect { x = left, y = top, w = right - left, h = bottom - top };
                                SDL.SDL_RenderCopy(Desktop.Renderer, SpritesheetTexture, ref sourceRect, ref destRect);
                            }
                        }
                    }
                }
            }

            foreach (var entity in state.SeenEntities)
            {
                if (entity.Position.X < startTileCoords.X || entity.Position.Y < startTileCoords.Y || entity.Position.X > endTileCoords.X || entity.Position.Y > endTileCoords.Y) continue;

                var left = ViewRectangle.X + (int)(entity.Position.X * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.X * _zoom);
                var right = ViewRectangle.X + (int)((entity.Position.X + 1) * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.X * _zoom);
                var top = ViewRectangle.Y + (int)(entity.Position.Y * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.Y * _zoom);
                var bottom = ViewRectangle.Y + (int)((entity.Position.Y + 1) * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.Y * _zoom);

                var sourceRect = new SDL.SDL_Rect { x = entity.SpriteLocation.X * Protocol.MapTileSize, y = entity.SpriteLocation.Y * Protocol.MapTileSize, w = Protocol.MapTileSize, h = Protocol.MapTileSize };
                var destRect = new SDL.SDL_Rect { x = left, y = top, w = right - left, h = bottom - top };
                SDL.SDL_RenderCopy(Desktop.Renderer, SpritesheetTexture, ref sourceRect, ref destRect);
            }

            var fogColor = new Color(0x00000044);
            fogColor.UseAsDrawColor(Desktop.Renderer);
            SDL.SDL_SetRenderDrawBlendMode(Desktop.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

            /*
            for (var y = startTileY; y <= endTileY; y++)
            {
                for (var x = startTileX; x <= endTileX; x++)
                {
                    var tileIndex = y * state.WorldSize.X + x;
                    if (fog[tileIndex] != 0) continue;

                    var rect = new SDL.SDL_Rect { x = x * TileSize - viewportScrollX, y = y * TileSize - viewportScrollY, w = TileSize, h = TileSize };
                    SDL.SDL_RenderFillRect(Desktop.Renderer, ref rect);
                }
            }
            */

            for (var chunkY = startChunkCoords.Y; chunkY <= endChunkCoords.Y; chunkY++)
            {
                for (var chunkX = startChunkCoords.X; chunkX <= endChunkCoords.X; chunkX++)
                {
                    var chunkStartTileCoords = new Point(chunkX * Protocol.MapChunkSide, chunkY * Protocol.MapChunkSide);

                    if (!state.FogChunks.TryGetValue(new Point(chunkX, chunkY), out var fogChunk))
                    {
                        var left = ViewRectangle.X + (int)(chunkStartTileCoords.X * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.X * _zoom);
                        var right = ViewRectangle.X + (int)((chunkStartTileCoords.X + Protocol.MapChunkSide) * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.X * _zoom);
                        var top = ViewRectangle.Y + (int)(chunkStartTileCoords.Y * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.Y * _zoom);
                        var bottom = ViewRectangle.Y + (int)((chunkStartTileCoords.Y + Protocol.MapChunkSide) * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.Y * _zoom);

                        var destRect = new SDL.SDL_Rect { x = left, y = top, w = right - left, h = bottom - top };
                        SDL.SDL_RenderFillRect(Desktop.Renderer, ref destRect);

                        continue;
                    }

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
                            if (fogChunk.TilesPerLayer[0][chunkRelativeY * Protocol.MapChunkSide + chunkRelativeX] != 0) continue;

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

            SDL.SDL_SetRenderDrawBlendMode(Desktop.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);

            if (state.SelectedEntity != null)
            {
                var color = new Color(0x00ff00ff);
                color.UseAsDrawColor(Desktop.Renderer);

                var x = state.SelectedEntity.Position.X;
                var y = state.SelectedEntity.Position.Y;
                var w = 1;
                var h = 1;

                var left = ViewRectangle.X + (int)(x * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.X * _zoom);
                var right = ViewRectangle.X + (int)(((x + w) * Protocol.MapTileSize - 1) * _zoom) - (int)(viewportScroll.X * _zoom);
                var top = ViewRectangle.Y + (int)(y * _zoom * Protocol.MapTileSize) - (int)(viewportScroll.Y * _zoom);
                var bottom = ViewRectangle.Y + (int)(((y + h) * Protocol.MapTileSize - 1) * _zoom) - (int)(viewportScroll.Y * _zoom);

                var rect = new Rectangle(left, top, right - left, bottom - top);

                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X, rect.Y, rect.X + rect.Width, rect.Y);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X + rect.Width, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X + rect.Width, rect.Y + rect.Height, rect.X, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X, rect.Y + rect.Height, rect.X, rect.Y);
            }
        }
        #endregion
    }
}
