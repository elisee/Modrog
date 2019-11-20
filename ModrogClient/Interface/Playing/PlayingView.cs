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

        bool _isUsingSlot0;
        bool _isUsingSlot1;

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
                (int)MathF.Floor((viewportScroll.X + (Desktop.MouseX - ViewRectangle.X) / _zoom) / App.State.TileSize),
                (int)MathF.Floor((viewportScroll.Y + (Desktop.MouseY - ViewRectangle.Y) / _zoom) / App.State.TileSize));

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

                if (key == SDL.SDL_Keycode.SDLK_a || key == SDL.SDL_Keycode.SDLK_q) _isScrollingLeft = true;
                if (key == SDL.SDL_Keycode.SDLK_d) _isScrollingRight = true;
                if (key == SDL.SDL_Keycode.SDLK_w || key == SDL.SDL_Keycode.SDLK_z) _isScrollingUp = true;
                if (key == SDL.SDL_Keycode.SDLK_s) _isScrollingDown = true;
            }

            if (key == SDL.SDL_Keycode.SDLK_x) _isUsingSlot0 = true;
            if (key == SDL.SDL_Keycode.SDLK_c) _isUsingSlot1 = true;

            var state = App.State;

            if (state.SelectedEntity != null && state.SelectedEntity.PlayerIndex == state.SelfPlayerIndex)
            {
                var intent = ModrogApi.EntityIntent.Move;
                var slot = _isUsingSlot0 ? 0 : (_isUsingSlot1 ? 1 : -1);
                if (slot != -1) intent = ModrogApi.EntityIntent.Use;

                var direction = key switch
                {
                    SDL.SDL_Keycode.SDLK_RIGHT => ModrogApi.Direction.Right,
                    SDL.SDL_Keycode.SDLK_DOWN => ModrogApi.Direction.Down,
                    SDL.SDL_Keycode.SDLK_LEFT => ModrogApi.Direction.Left,
                    SDL.SDL_Keycode.SDLK_UP => ModrogApi.Direction.Up,
                    _ => (ModrogApi.Direction?)null,
                };

                if (direction.HasValue) state.SetIntent(intent, direction.Value, slot);
            }

            if (key == SDL.SDL_Keycode.SDLK_TAB)
            {
                _sidebarPanel.Visible = true;
                _sidebarPanel.Layout(_contentRectangle);
            }
        }

        public override void OnKeyUp(SDL.SDL_Keycode key)
        {
            if (key == SDL.SDL_Keycode.SDLK_a || key == SDL.SDL_Keycode.SDLK_q) _isScrollingLeft = false;
            if (key == SDL.SDL_Keycode.SDLK_d) _isScrollingRight = false;
            if (key == SDL.SDL_Keycode.SDLK_w || key == SDL.SDL_Keycode.SDLK_z) _isScrollingUp = false;
            if (key == SDL.SDL_Keycode.SDLK_s) _isScrollingDown = false;

            if (key == SDL.SDL_Keycode.SDLK_x) _isUsingSlot0 = false;
            if (key == SDL.SDL_Keycode.SDLK_c) _isUsingSlot1 = false;

            var state = App.State;
            if (key == SDL.SDL_Keycode.SDLK_LEFT) state.ClearMoveIntent(ModrogApi.Direction.Left);
            if (key == SDL.SDL_Keycode.SDLK_RIGHT) state.ClearMoveIntent(ModrogApi.Direction.Right);
            if (key == SDL.SDL_Keycode.SDLK_UP) state.ClearMoveIntent(ModrogApi.Direction.Up);
            if (key == SDL.SDL_Keycode.SDLK_DOWN) state.ClearMoveIntent(ModrogApi.Direction.Down);

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

                foreach (var entity in App.State.EntitiesInSight)
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
            if (Desktop.HasNoKeyModifier || Desktop.IsShiftOnlyDown)
            {
                if (!Desktop.IsShiftDown) _zoom = Math.Clamp(_zoom + dy / 10f, MinZoom, MaxZoom);
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

                new Element(panel) { Width = 12, Height = 12, Right = 8, BackgroundPatch = new TexturePatch(player.IsHost ? 0xffff44ff : 0x00000000) };
                new Element(panel) { Width = 12, Height = 12, Right = 8, BackgroundPatch = new TexturePatch(player.IsOnline ? 0x44ff44ff : 0xff4444ff) };
                new Label(panel) { LayoutWeight = 1, Text = player.Name };
            }

            _playerListContainer.Layout();
        }

        public void OnChatMessageReceived(string author, string message)
        {
            // TODO: Display chat message
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
            Scroll = new Vector2(position.X * App.State.TileSize, position.Y * App.State.TileSize);
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
                (int)MathF.Floor(viewportScroll.X / state.TileSize),
                (int)MathF.Floor(viewportScroll.Y / state.TileSize));

            var endTileCoords = new Point(
                startTileCoords.X + (int)MathF.Ceiling(ViewRectangle.Width / (state.TileSize * _zoom) + 1),
                startTileCoords.Y + (int)MathF.Ceiling(ViewRectangle.Height / (state.TileSize * _zoom) + 1));

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

                                var left = ViewRectangle.X + (int)(x * _zoom * state.TileSize) - (int)(viewportScroll.X * _zoom);
                                var right = ViewRectangle.X + (int)((x + 1) * _zoom * state.TileSize) - (int)(viewportScroll.X * _zoom);
                                var top = ViewRectangle.Y + (int)(y * _zoom * state.TileSize) - (int)(viewportScroll.Y * _zoom);
                                var bottom = ViewRectangle.Y + (int)((y + 1) * _zoom * state.TileSize) - (int)(viewportScroll.Y * _zoom);

                                var spriteLocation = tileKinds[tile - 1].SpriteLocation;
                                var sourceRect = new SDL.SDL_Rect { x = spriteLocation.X * state.TileSize, y = spriteLocation.Y * state.TileSize, w = state.TileSize, h = state.TileSize };
                                var destRect = new SDL.SDL_Rect { x = left, y = top, w = right - left, h = bottom - top };
                                SDL.SDL_RenderCopy(Desktop.Renderer, SpritesheetTexture, ref sourceRect, ref destRect);
                            }
                        }
                    }
                }
            }

            var tickProgress = state.TickProgress;

            foreach (var entity in state.EntitiesInSight)
            {
                if (entity.Position.X < startTileCoords.X || entity.Position.Y < startTileCoords.Y || entity.Position.X > endTileCoords.X || entity.Position.Y > endTileCoords.Y) continue;

                var position = Vector2.Lerp(entity.PreviousTickPosition.ToVector2(), entity.Position.ToVector2(), tickProgress);
                var angle = ModrogApi.MathHelper.GetAngleFromDirection(entity.ActionDirection);
                var stretch = Vector2.One;

                if (entity.Action == ModrogApi.EntityAction.Move)
                {
                    position.Y += -MathF.Sin(tickProgress * MathF.PI) * 0.2f;
                    stretch.X = 1f - MathF.Sin(tickProgress * MathF.PI) * 0.2f;
                    stretch.Y = 1f + MathF.Sin(tickProgress * MathF.PI) * 0.2f;
                }
                else if (entity.Action == ModrogApi.EntityAction.Bounce)
                {
                    var amount = MathF.Sin(tickProgress * MathF.PI) * 0.1f;

                    position.X += MathF.Cos(angle) * amount;
                    position.Y += MathF.Sin(angle) * amount;

                    stretch.X = 1f - Math.Abs(MathF.Cos(angle)) * amount + Math.Abs(MathF.Sin(angle)) * amount;
                    stretch.Y = 1f - Math.Abs(MathF.Sin(angle)) * amount + Math.Abs(MathF.Cos(angle)) * amount;
                }


                var left = ViewRectangle.X + (int)((position.X + 0.5f - 0.5f * stretch.X) * _zoom * state.TileSize) - (int)(viewportScroll.X * _zoom);
                var right = ViewRectangle.X + (int)((position.X + 0.5f + 0.5f * stretch.X) * _zoom * state.TileSize) - (int)(viewportScroll.X * _zoom);
                var top = ViewRectangle.Y + (int)((position.Y + 0.5f - 0.5f * stretch.Y) * _zoom * state.TileSize) - (int)(viewportScroll.Y * _zoom);
                var bottom = ViewRectangle.Y + (int)((position.Y + 0.5f + 0.5f * stretch.Y) * _zoom * state.TileSize) - (int)(viewportScroll.Y * _zoom);

                var sourceRect = new SDL.SDL_Rect { x = entity.SpriteLocation.X * state.TileSize, y = entity.SpriteLocation.Y * state.TileSize, w = state.TileSize, h = state.TileSize };
                var destRect = new SDL.SDL_Rect { x = left, y = top, w = right - left, h = bottom - top };
                SDL.SDL_RenderCopyEx(Desktop.Renderer, SpritesheetTexture, ref sourceRect, ref destRect, 0f, IntPtr.Zero, entity.ActionDirection == ModrogApi.Direction.Left ? SDL.SDL_RendererFlip.SDL_FLIP_HORIZONTAL : SDL.SDL_RendererFlip.SDL_FLIP_NONE);
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
                        var left = ViewRectangle.X + (int)(chunkStartTileCoords.X * _zoom * state.TileSize) - (int)(viewportScroll.X * _zoom);
                        var right = ViewRectangle.X + (int)((chunkStartTileCoords.X + Protocol.MapChunkSide) * _zoom * state.TileSize) - (int)(viewportScroll.X * _zoom);
                        var top = ViewRectangle.Y + (int)(chunkStartTileCoords.Y * _zoom * state.TileSize) - (int)(viewportScroll.Y * _zoom);
                        var bottom = ViewRectangle.Y + (int)((chunkStartTileCoords.Y + Protocol.MapChunkSide) * _zoom * state.TileSize) - (int)(viewportScroll.Y * _zoom);

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

                            var left = ViewRectangle.X + (int)(x * _zoom * state.TileSize) - (int)(viewportScroll.X * _zoom);
                            var right = ViewRectangle.X + (int)((x + 1) * _zoom * state.TileSize) - (int)(viewportScroll.X * _zoom);
                            var top = ViewRectangle.Y + (int)(y * _zoom * state.TileSize) - (int)(viewportScroll.Y * _zoom);
                            var bottom = ViewRectangle.Y + (int)((y + 1) * _zoom * state.TileSize) - (int)(viewportScroll.Y * _zoom);

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

                var position = Vector2.Lerp(state.SelectedEntity.PreviousTickPosition.ToVector2(), state.SelectedEntity.Position.ToVector2(), tickProgress);
                var size = new Vector2(1f, 1f);

                var left = ViewRectangle.X + (int)(position.X * _zoom * state.TileSize) - (int)(viewportScroll.X * _zoom);
                var right = ViewRectangle.X + (int)(((position.X + size.X) * state.TileSize) * _zoom) - (int)(viewportScroll.X * _zoom);
                var top = ViewRectangle.Y + (int)(position.Y * _zoom * state.TileSize) - (int)(viewportScroll.Y * _zoom);
                var bottom = ViewRectangle.Y + (int)(((position.Y + size.Y) * state.TileSize) * _zoom) - (int)(viewportScroll.Y * _zoom);

                var rect = new Rectangle(left, top, right - left, bottom - top).ToSDL_Rect();
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref rect);
            }
        }
    }
    #endregion
}
