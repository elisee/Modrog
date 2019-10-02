using DeepSwarmClient.UI;
using DeepSwarmCommon;
using SDL2;
using System;
using System.Collections.Generic;

namespace DeepSwarmClient.Interface.Playing
{
    class PlayingView : InterfaceElement
    {
        // UI
        readonly Element _playerListPanel;

        const int EntityStatsContainerWidth = 300;
        readonly Element _entityStatsContainer;
        readonly Label _entityNameLabel;
        readonly Label _entityOwnerLabel;
        readonly Label _entityHealthValue;
        readonly Label _entityCrystalsValue;

        const int SidebarPanelWidth = 400;
        readonly Element _sidebarContainer;

        readonly Dictionary<Entity.EntityType, Element> _manualModeSidebarsByEntityType;

        readonly Element _scriptSelectorSidebar;
        readonly Element _scriptSelectorList;

        readonly Element _scriptEditorSidebar;

        readonly Label _scriptNameLabel;
        readonly Button _renameScriptButton;
        readonly RenameScriptPopup _renameScriptPopup;

        readonly TextEditor _scriptTextEditor;

        // Hovered tile
        int _hoveredTileX;
        int _hoveredTileY;

        // Scrolling
        float _scrollingPixelsX;
        float _scrollingPixelsY;

        bool _isScrollingLeft;
        bool _isScrollingRight;
        bool _isScrollingUp;
        bool _isScrollingDown;

        bool _isDraggingScroll;
        int _dragScrollX;
        int _dragScrollY;

        public PlayingView(Interface @interface)
            : base(@interface, null)
        {
            // TODO(ui):
            /*
            _playerListPanel = new Panel(null, new TexturePatch(0x123456ff))
            {
                Anchor = new Anchor(left: 0, top: 0, width: (Protocol.MaxPlayerNameLength + 2) * 16, height: 720),
            };

            // Entity stats

            _entityStatsContainer = new Element(Desktop)
            {
                Anchor = new Anchor(left: (Engine.Viewport.Width - EntityStatsContainerWidth) / 2, top: 24, width: EntityStatsContainerWidth, height: 96),
                BackgroundPatch = new TexturePatch(0x123456ff)
            };

            _entityNameLabel = new Label(_entityStatsContainer)
            {
                Anchor = new Anchor(left: 0, top: 6, width: EntityStatsContainerWidth, height: 16)
            };

            _entityOwnerLabel = new Label(_entityStatsContainer)
            {
                Anchor = new Anchor(left: 0, top: 28, width: EntityStatsContainerWidth, height: 16),
                TextColor = new Color(0x888888ff)
            };

            // Health icon
            new Element(_entityStatsContainer)
            {
                Anchor = new Anchor(left: 12, top: 56, width: 24, height: 24),
                BackgroundPatch = new TexturePatch(new TextureArea(Engine.SpritesheetTexture, new Rectangle(72, 24, 24, 24)))
            };

            _entityHealthValue = new Label(_entityStatsContainer)
            {
                Anchor = new Anchor(left: 48, top: 60, width: EntityStatsContainerWidth, height: 16)
            };

            // Crystals icon
            new Element(_entityStatsContainer)
            {
                Anchor = new Anchor(left: EntityStatsContainerWidth / 2 + 12, top: 56, width: 24, height: 24),
                BackgroundPatch = new TexturePatch(new TextureArea(Engine.SpritesheetTexture, new Rectangle(96, 24, 24, 24)))
            };

            _entityCrystalsValue = new Label(_entityStatsContainer)
            {
                Anchor = new Anchor(left: EntityStatsContainerWidth / 2 + 48, top: 60, width: EntityStatsContainerWidth, height: 16)
            };

            // Sidebar
            _sidebarContainer = new Element(this)
            {
                Anchor = new Anchor(left: 0, top: 0, width: Engine.Viewport.Width, height: Engine.Viewport.Height)
            };

            const int ButtonStripWidth = (128 + 2 * 8);

            Element activeStrip = null;
            int stripButtons = 0;

            Element StartButtonStrip(Element strip)
            {
                activeStrip = strip;
                stripButtons = 0;
                return strip;
            }

            void AddButtonToStrip(string label, Action action)
            {
                var buttonHeight = 64;

                new TextButtonactiveStrip)
                {
                    Text = label,
                    Anchor = new Anchor(left: 8, top: 8 + stripButtons * (buttonHeight + 8), width: activeStrip.Anchor.Width - 16, height: buttonHeight),
                    BackgroundPatch = new TexturePatch(0x4444ccff),
                    OnActivate = action
                };

                stripButtons++;
            }

            // Manual mode
            _manualModeSidebarsByEntityType = new Dictionary<Entity.EntityType, Element>();

            _manualModeSidebarsByEntityType[Entity.EntityType.Heart] = new Element(Desktop);

            _manualModeSidebarsByEntityType[Entity.EntityType.Factory] = new Element(Desktop)
            {
                Anchor = new Anchor(left: Engine.Viewport.Width - ButtonStripWidth, top: 0, width: ButtonStripWidth, height: Engine.Viewport.Height),
            };

            StartButtonStrip(_manualModeSidebarsByEntityType[Entity.EntityType.Factory]);
            AddButtonToStrip("SCRIPT", () => Engine.State.SetupScriptPathForSelectedEntity(null));
            AddButtonToStrip("BUILD", () => Engine.State.PlanMove(Entity.EntityMove.Build));

            _manualModeSidebarsByEntityType[Entity.EntityType.Robot] = new Element(Desktop)
            {
                Anchor = new Anchor(left: Engine.Viewport.Width - ButtonStripWidth, top: 0, width: ButtonStripWidth, height: Engine.Viewport.Height),
            };

            StartButtonStrip(_manualModeSidebarsByEntityType[Entity.EntityType.Robot]);
            AddButtonToStrip("SCRIPT", () => Engine.State.SetupScriptPathForSelectedEntity(null));
            AddButtonToStrip("CW", () => Engine.State.PlanMove(Entity.EntityMove.RotateCW));
            AddButtonToStrip("MOVE", () => Engine.State.PlanMove(Entity.EntityMove.Forward));
            AddButtonToStrip("CCW", () => Engine.State.PlanMove(Entity.EntityMove.RotateCCW));

            // Script selector
            _scriptSelectorSidebar = new Element(Desktop)
            {
                Anchor = new Anchor(left: Engine.Viewport.Width - ButtonStripWidth - SidebarPanelWidth, top: 0, width: ButtonStripWidth + SidebarPanelWidth, height: Engine.Viewport.Height)
            };

            var scriptSelectorButtonStrip = StartButtonStrip(new Element(_scriptSelectorSidebar)
            {
                Anchor = new Anchor(left: 0, top: 0, width: ButtonStripWidth, height: Engine.Viewport.Height)
            });

            AddButtonToStrip("MANUAL", () => Engine.State.ClearScriptPathForSelectedEntity());

            var scriptSelectorPanel = new Panel(_scriptSelectorSidebar, new TexturePatch(0x123456ff))
            {
                Anchor = new Anchor(left: ButtonStripWidth, top: 0, width: SidebarPanelWidth, height: Engine.Viewport.Height),
            };

            new TextButton(scriptSelectorPanel)
            {
                Anchor = new Anchor(left: 8, top: 8, width: SidebarPanelWidth - 16, height: 16),
                Text = "[+] New Script",
                OnActivate = () => Engine.State.CreateScriptForSelectedEntity()
            };

            _scriptSelectorList = new Element(scriptSelectorPanel)
            {
                Anchor = new Anchor(left: 8, top: 32 + 8, width: SidebarPanelWidth - 16, height: Engine.Viewport.Height - 32 - 16)
            };

            // Script editor
            _scriptEditorSidebar = new Element(Desktop)
            {
                Anchor = new Anchor(left: Engine.Viewport.Width - ButtonStripWidth - SidebarPanelWidth, top: 0, width: ButtonStripWidth + SidebarPanelWidth, height: Engine.Viewport.Height),
            };

            var scriptEditorButtonStrip = StartButtonStrip(new Element(_scriptEditorSidebar)
            {
                Anchor = new Anchor(left: 0, top: 0, width: ButtonStripWidth, height: Engine.Viewport.Height)
            });

            AddButtonToStrip("STOP", () => Engine.State.SetupScriptPathForSelectedEntity(null));
            AddButtonToStrip("SAVE", () => Engine.State.UpdateSelectedEntityScript(_scriptTextEditor.GetText()));

            var scriptEditorPanel = new Panel(_scriptEditorSidebar, new TexturePatch(0x123456ff))
            {
                Anchor = new Anchor(left: ButtonStripWidth, top: 0, width: SidebarPanelWidth, height: Engine.Viewport.Height),
            };

            _renameScriptButton = new TextButton(scriptEditorPanel)
            {
                Anchor = new Anchor(right: 8, top: 8, width: " Rename ".Length * 16, height: 16),
                Text = " Rename ",
                OnActivate = () => Engine.Interface.SetPopup(_renameScriptPopup)
            };

            _scriptNameLabel = new Label(scriptEditorPanel)
            {
                Anchor = new Anchor(left: 8, top: 8, right: _renameScriptButton.Anchor.Width + 16, height: 16),
                BackgroundPatch = new TexturePatch(0x004400ff),
            };

            _renameScriptPopup = new RenameScriptPopup(@interface);

            _scriptTextEditor = new TextEditor(scriptEditorPanel)
            {
                Anchor = new Anchor(left: 8, top: 8 + 16 + 8, width: SidebarPanelWidth - 16, height: Engine.Viewport.Height - (8 + 16 + 8 + 8)),
                BackgroundPatch = new TexturePatch(0x004400ff),
            };

            OnScriptListUpdated();
            */
        }

        public override Element HitTest(int x, int y)
        {
            return base.HitTest(x, y) ?? (LayoutRectangle.Contains(x, y) ? this : null);
        }

        public override void OnMounted()
        {
            _scrollingPixelsX = (int)((Engine.State.SelfBaseChunkX + 0.5f) * Map.ChunkSize * Map.TileSize) - Engine.Viewport.Width / 2;
            _scrollingPixelsY = (int)((Engine.State.SelfBaseChunkY + 0.5f) * Map.ChunkSize * Map.TileSize) - Engine.Viewport.Height / 2;

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

            if (key == SDL.SDL_Keycode.SDLK_TAB)
            {
                Add(_playerListPanel);
                _playerListPanel.Layout(LayoutRectangle);
            }

            if (!_isDraggingScroll)
            {
                if (key == SDL.SDL_Keycode.SDLK_LEFT) _isScrollingLeft = true;
                if (key == SDL.SDL_Keycode.SDLK_RIGHT) _isScrollingRight = true;
                if (key == SDL.SDL_Keycode.SDLK_UP) _isScrollingUp = true;
                if (key == SDL.SDL_Keycode.SDLK_DOWN) _isScrollingDown = true;
            }

            if (Engine.State.SelectedEntity != null && !Engine.State.LuasByEntityId.ContainsKey(Engine.State.SelectedEntity.Id))
            {
                if (key == SDL.SDL_Keycode.SDLK_a || key == SDL.SDL_Keycode.SDLK_q) Engine.State.SetMoveTowards(Entity.EntityDirection.Left);
                if (key == SDL.SDL_Keycode.SDLK_d) Engine.State.SetMoveTowards(Entity.EntityDirection.Right);
                if (key == SDL.SDL_Keycode.SDLK_w || key == SDL.SDL_Keycode.SDLK_z) Engine.State.SetMoveTowards(Entity.EntityDirection.Up);
                if (key == SDL.SDL_Keycode.SDLK_s) Engine.State.SetMoveTowards(Entity.EntityDirection.Down);
            }
        }

        public override void OnKeyUp(SDL.SDL_Keycode key)
        {
            if (key == SDL.SDL_Keycode.SDLK_TAB)
            {
                Remove(_playerListPanel);
            }

            if (key == SDL.SDL_Keycode.SDLK_LEFT) _isScrollingLeft = false;
            if (key == SDL.SDL_Keycode.SDLK_RIGHT) _isScrollingRight = false;
            if (key == SDL.SDL_Keycode.SDLK_UP) _isScrollingUp = false;
            if (key == SDL.SDL_Keycode.SDLK_DOWN) _isScrollingDown = false;

            if (key == SDL.SDL_Keycode.SDLK_a || key == SDL.SDL_Keycode.SDLK_q) Engine.State.StopMovingTowards(Entity.EntityDirection.Left);
            if (key == SDL.SDL_Keycode.SDLK_d) Engine.State.StopMovingTowards(Entity.EntityDirection.Right);
            if (key == SDL.SDL_Keycode.SDLK_w || key == SDL.SDL_Keycode.SDLK_z) Engine.State.StopMovingTowards(Entity.EntityDirection.Up);
            if (key == SDL.SDL_Keycode.SDLK_s) Engine.State.StopMovingTowards(Entity.EntityDirection.Down);
        }

        public override void OnMouseMove()
        {
            if (_isDraggingScroll)
            {
                _scrollingPixelsX = _dragScrollX - Desktop.MouseX;
                _scrollingPixelsY = _dragScrollY - Desktop.MouseY;
            }
        }

        public override void OnMouseDown(int button)
        {
            if (button == 1)
            {
                var startTileX = (int)_scrollingPixelsX / Map.TileSize;
                var startTileY = (int)_scrollingPixelsY / Map.TileSize;

                var hoveredEntities = new List<Entity>();

                foreach (var entity in Engine.State.Map.Entities)
                {
                    var tileX = Map.Wrap(entity.X - startTileX) + startTileX;
                    var tileY = Map.Wrap(entity.Y - startTileY) + startTileY;

                    if (tileX == _hoveredTileX && tileY == _hoveredTileY) hoveredEntities.Add(entity);
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
                _dragScrollX = (int)_scrollingPixelsX + Desktop.MouseX;
                _dragScrollY = (int)_scrollingPixelsY + Desktop.MouseY;
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
            _playerListPanel.Clear();

            for (var i = 0; i < Engine.State.PlayerList.Count; i++)
            {
                var label = new Label(_playerListPanel) { Text = PlayerListEntry.GetEntryLabel(Engine.State.PlayerList[i]) };
                // TODO(ui): label.Anchor = new Anchor(left: 16, top: 16 + 16 * i, width: _playerListPanel.Anchor.Width, height: 16);
            }

            _playerListPanel.Layout(LayoutRectangle);
        }

        public void OnScriptListUpdated()
        {
            _scriptSelectorList.Clear();

            var keys = new List<string>(Engine.State.Scripts.Keys);
            keys.Sort((a, b) => string.Compare(a, b));

            var i = 0;
            foreach (var scriptPath in keys)
            {
                new TextButton(_scriptSelectorList)
                {
                    // TODO(ui): Anchor = new Anchor(left: 0, top: i * 16, width: _scriptSelectorList.Anchor.Width, height: 16),
                    Text = scriptPath,
                    OnActivate = () => Engine.State.SetupScriptPathForSelectedEntity(scriptPath)
                };

                i++;
            }

            _scriptSelectorList.Layout(_scriptSelectorList.Parent.LayoutRectangle);
        }

        public void OnSelectedEntityChanged()
        {
            Desktop.SetFocusedElement(this);

            if (_entityStatsContainer.Parent != null) Remove(_entityStatsContainer);
            _sidebarContainer.Clear();

            var selectedEntity = Engine.State.SelectedEntity;
            if (selectedEntity == null) return;

            _entityNameLabel.Text = selectedEntity.Type.ToString();
            // TODO(ui): _entityNameLabel.Anchor.Left = (EntityStatsContainerWidth - entityNameLength) / 2;

            _entityOwnerLabel.Text = PlayerListEntry.GetEntryLabel(Engine.State.PlayerList[selectedEntity.PlayerIndex]);
            // TODO(ui): _entityOwnerLabel.Anchor.Left = (EntityStatsContainerWidth - entityOwnerLength) / 2;

            Add(_entityStatsContainer);
            OnSelectedEntityUpdated();

            if (Engine.State.EntityScriptPaths.TryGetValue(selectedEntity.Id, out var scriptPath))
            {
                // TODO(ui): _entityStatsContainer.Anchor.Left = (Engine.Viewport.Width - EntityStatsContainerWidth - SidebarPanelWidth) / 2;

                if (scriptPath == null)
                {
                    _sidebarContainer.Add(_scriptSelectorSidebar);
                }
                else
                {
                    _sidebarContainer.Add(_scriptEditorSidebar);
                    _scriptNameLabel.Text = scriptPath;
                    _scriptTextEditor.SetText(Engine.State.Scripts[scriptPath]);
                }
            }
            else
            {
                // TODO(ui): _entityStatsContainer.Anchor.Left = (Engine.Viewport.Width - EntityStatsContainerWidth) / 2;

                _sidebarContainer.Add(_manualModeSidebarsByEntityType[selectedEntity.Type]);
            }

            _entityStatsContainer.Layout(Engine.Viewport);

            _sidebarContainer.Layout(Engine.Viewport);
        }

        public void OnSelectedEntityUpdated()
        {
            var selectedEntity = Engine.State.SelectedEntity;
            _entityHealthValue.Text = selectedEntity.Health.ToString();
            _entityCrystalsValue.Text = selectedEntity.Crystals.ToString();
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

            _hoveredTileX = ((int)_scrollingPixelsX + Desktop.MouseX) / Map.TileSize;
            _hoveredTileY = ((int)_scrollingPixelsY + Desktop.MouseY) / Map.TileSize;
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            var startTileX = (int)_scrollingPixelsX / Map.TileSize;
            var startTileY = (int)_scrollingPixelsY / Map.TileSize;

            var tilesPerRow = (int)MathF.Ceiling((float)Engine.Viewport.Width / Map.TileSize + 1);
            var tilesPerColumn = (int)MathF.Ceiling((float)Engine.Viewport.Height / Map.TileSize + 1);

            var map = Engine.State.Map;

            // Draw tiles
            for (var y = -1; y < tilesPerColumn; y++)
            {
                for (var x = -1; x < tilesPerRow; x++)
                {
                    var tileX = Map.Wrap(startTileX + x);
                    var tileY = Map.Wrap(startTileY + y);
                    var sourceRect = RendererHelper.GetTileRenderSourceRect(tileX, tileY, map);

                    var renderX = (startTileX + x) * Map.TileSize - (int)_scrollingPixelsX;
                    var renderY = (startTileY + y) * Map.TileSize - (int)_scrollingPixelsY;
                    var destRect = new SDL.SDL_Rect() { x = renderX, y = renderY, w = Map.TileSize, h = Map.TileSize };

                    SDL.SDL_RenderCopy(Engine.Renderer, Engine.SpritesheetTexture, ref sourceRect, ref destRect);
                }
            }

            // Draw fog of war
            var fogOfWar = Engine.State.FogOfWar;
            var fogColor = new Color(0x00000044);
            fogColor.UseAsDrawColor(Engine.Renderer);
            SDL.SDL_SetRenderDrawBlendMode(Engine.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

            for (var y = -1; y < tilesPerColumn; y++)
            {
                for (var x = -1; x < tilesPerRow; x++)
                {
                    var tileX = Map.Wrap(startTileX + x);
                    var tileY = Map.Wrap(startTileY + y);

                    var tileIndex = tileY * Map.MapSize + tileX;
                    if (fogOfWar[tileIndex] != 0) continue;

                    var renderX = (startTileX + x) * Map.TileSize - (int)_scrollingPixelsX;
                    var renderY = (startTileY + y) * Map.TileSize - (int)_scrollingPixelsY;

                    var rect = Desktop.ToSDL_Rect(new Rectangle(renderX, renderY, Map.TileSize, Map.TileSize));
                    SDL.SDL_RenderFillRect(Engine.Renderer, ref rect);
                }
            }

            SDL.SDL_SetRenderDrawBlendMode(Engine.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);

            // Draw entities
            foreach (var entity in map.Entities)
            {
                var tileX = Map.Wrap(entity.X - startTileX) + startTileX;
                var tileY = Map.Wrap(entity.Y - startTileY) + startTileY;

                var renderX = tileX * Map.TileSize - (int)_scrollingPixelsX;
                var renderY = tileY * Map.TileSize - (int)_scrollingPixelsY;

                switch (entity.Type)
                {
                    case Entity.EntityType.Factory:
                    case Entity.EntityType.Heart:
                    case Entity.EntityType.Robot:
                        {
                            RendererHelper.GetEntityRenderRects(entity, out var sourceRect, out var destRect);
                            destRect.x += renderX;
                            destRect.y += renderY;

                            if (destRect.x + destRect.w < 0 || destRect.x > Engine.Viewport.Width) continue;
                            if (destRect.y + destRect.h < 0 || destRect.y > Engine.Viewport.Height) continue;

                            SDL.SDL_RenderCopy(Engine.Renderer, Engine.SpritesheetTexture, ref sourceRect, ref destRect);
                            break;
                        }

                    default:
                        {
                            var stats = Entity.EntityStatsByType[(int)entity.Type];
                            var color = new Color(stats.NeutralColor);
                            if (entity.PlayerIndex != -1) color.RGBA = stats.BlueColor;

                            color.UseAsDrawColor(Engine.Renderer);

                            var destRect = Desktop.ToSDL_Rect(new Rectangle(renderX, renderY, Map.TileSize, Map.TileSize));
                            if (destRect.x + destRect.w < 0 || destRect.x > Engine.Viewport.Width) continue;
                            if (destRect.y + destRect.h < 0 || destRect.y > Engine.Viewport.Height) continue;

                            SDL.SDL_RenderFillRect(Engine.Renderer, ref destRect);
                            break;
                        }
                }

            }

            // Draw box around selected entity
            var selectedEntity = Engine.State.SelectedEntity;
            if (selectedEntity != null)
            {
                var color = new Color(0x00ff00ff);
                color.UseAsDrawColor(Engine.Renderer);

                var x = selectedEntity.X;
                var y = selectedEntity.Y;
                var w = 1;
                var h = 1;

                switch (selectedEntity.Type)
                {
                    case Entity.EntityType.Factory: x -= 1; y -= 1; w = 3; h = 2; break;
                }

                var tileX = Map.Wrap(x - startTileX) + startTileX;
                var tileY = Map.Wrap(y - startTileY) + startTileY;

                var renderX = tileX * Map.TileSize - (int)_scrollingPixelsX;
                var renderY = tileY * Map.TileSize - (int)_scrollingPixelsY;

                var rect = new Rectangle(renderX, renderY, w * Map.TileSize, h * Map.TileSize);

                SDL.SDL_RenderDrawLine(Engine.Renderer, rect.X, rect.Y, rect.X + rect.Width, rect.Y);
                SDL.SDL_RenderDrawLine(Engine.Renderer, rect.X + rect.Width, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Engine.Renderer, rect.X + rect.Width, rect.Y + rect.Height, rect.X, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Engine.Renderer, rect.X, rect.Y + rect.Height, rect.X, rect.Y);
            }
        }
    }
}
