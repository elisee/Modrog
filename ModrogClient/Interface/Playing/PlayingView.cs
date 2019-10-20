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
            if (_isDraggingScroll)
            {
                ScrollingPixelsX = _dragScroll.X - Desktop.MouseX;
                ScrollingPixelsY = _dragScroll.Y - Desktop.MouseY;
            }
        }

        public override void OnMouseDown(int button)
        {
            if (button == SDL.SDL_BUTTON_LEFT)
            {
                var startTileX = (int)ScrollingPixelsX / TileSize;
                var startTileY = (int)ScrollingPixelsY / TileSize;

                var hoveredEntities = new List<Game.ClientEntity>();

                foreach (var entity in App.State.SeenEntities)
                {
                    if (entity.Position.X == _hoveredTileX && entity.Position.Y == _hoveredTileY) hoveredEntities.Add(entity);
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
                _dragScroll.X = (int)ScrollingPixelsX + Desktop.MouseX;
                _dragScroll.Y = (int)ScrollingPixelsY + Desktop.MouseY;
            }
        }

        public override void OnMouseUp(int button)
        {
            if (button == SDL.SDL_BUTTON_MIDDLE)
            {
                _isDraggingScroll = false;
            }
        }

        public override void OnMouseWheel(int dx, int dy)
        {
            ScrollingPixelsX += dx * 12;
            ScrollingPixelsY -= dy * 12;
        }

        void Animate(float deltaTime)
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

            var viewportScrollX = -ViewRectangle.Width / 2 + (int)ScrollingPixelsX;
            var viewportScrollY = -ViewRectangle.Height / 2 + (int)ScrollingPixelsY;

            _hoveredTileX = ((int)viewportScrollX + Desktop.MouseX) / TileSize;
            _hoveredTileY = ((int)viewportScrollY + Desktop.MouseY) / TileSize;
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
            ScrollingPixelsX = position.X * TileSize;
            ScrollingPixelsY = position.Y * TileSize;
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
            if (state.WorldSize.X == 0 || state.WorldSize.Y == 0) return;

            var viewportScrollX = -ViewRectangle.Width / 2 + (int)ScrollingPixelsX;
            var viewportScrollY = -ViewRectangle.Height / 2 + (int)ScrollingPixelsY;

            var startTileX = Math.Max(0, (int)viewportScrollX / TileSize);
            var startTileY = Math.Max(0, (int)viewportScrollY / TileSize);

            var endTileX = Math.Min(state.WorldSize.X - 1, startTileX + (int)MathF.Ceiling((float)ViewRectangle.Width / TileSize + 1));
            var endTileY = Math.Min(state.WorldSize.Y - 1, startTileY + (int)MathF.Ceiling((float)ViewRectangle.Height / TileSize + 1));

            new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);

            for (var y = startTileY; y <= endTileY; y++)
            {
                for (var x = startTileX; x <= endTileX; x++)
                {
                    var tile = state.WorldTiles[y * state.WorldSize.X + x];

                    if (tile != 0)
                    {
                        var spriteLocation = state.TileKinds[tile].SpriteLocation;
                        var sourceRect = new SDL.SDL_Rect { x = spriteLocation.X * TileSize, y = spriteLocation.Y * TileSize, w = TileSize, h = TileSize };
                        var destRect = new SDL.SDL_Rect { x = x * TileSize - viewportScrollX, y = y * TileSize - viewportScrollY, w = TileSize, h = TileSize };
                        SDL.SDL_RenderCopy(Desktop.Renderer, SpritesheetTexture, ref sourceRect, ref destRect);
                    }
                }
            }

            foreach (var entity in state.SeenEntities)
            {
                if (entity.Position.X < startTileX || entity.Position.Y < startTileY || entity.Position.X > endTileX || entity.Position.Y > endTileY) continue;

                var sourceRect = new SDL.SDL_Rect { x = entity.SpriteLocation.X * TileSize, y = entity.SpriteLocation.Y * TileSize, w = TileSize, h = TileSize };
                var destRect = new SDL.SDL_Rect { x = entity.Position.X * TileSize - viewportScrollX, y = entity.Position.Y * TileSize - viewportScrollY, w = TileSize, h = TileSize };
                SDL.SDL_RenderCopy(Desktop.Renderer, SpritesheetTexture, ref sourceRect, ref destRect);
            }

            var fog = App.State.WorldFog;
            var fogColor = new Color(0x00000044);
            fogColor.UseAsDrawColor(Desktop.Renderer);
            SDL.SDL_SetRenderDrawBlendMode(Desktop.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

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

            SDL.SDL_SetRenderDrawBlendMode(Desktop.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);

            if (state.SelectedEntity != null)
            {
                var color = new Color(0x00ff00ff);
                color.UseAsDrawColor(Desktop.Renderer);

                var x = state.SelectedEntity.Position.X;
                var y = state.SelectedEntity.Position.Y;
                var w = 1;
                var h = 1;

                var renderX = x * TileSize - viewportScrollX;
                var renderY = y * TileSize - viewportScrollY;

                var rect = new Rectangle(renderX, renderY, w * TileSize, h * TileSize);

                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X, rect.Y, rect.X + rect.Width, rect.Y);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X + rect.Width, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X + rect.Width, rect.Y + rect.Height, rect.X, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Desktop.Renderer, rect.X, rect.Y + rect.Height, rect.X, rect.Y);
            }
        }
        #endregion
    }
}
