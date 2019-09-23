﻿using DeepSwarmClient.UI;
using DeepSwarmCommon;
using SDL2;
using System;

namespace DeepSwarmClient
{
    class InGameView : EngineElement
    {
        readonly Element _playerListPanel;

        readonly Element _sidebarContainer;

        readonly Element _manualModeSidebar;

        readonly Element _scriptSelectorSidebar;
        readonly Element _scriptSelectorList;

        readonly Element _scriptEditorSidebar;
        readonly TextInput _scriptNameInput;
        readonly TextEditor _scriptTextEditor;

        public InGameView(Engine engine)
            : base(engine, null)
        {
            AnchorRectangle = engine.Viewport;

            _playerListPanel = new Element(Desktop, null)
            {
                AnchorRectangle = new Rectangle(0, 0, (Protocol.MaxPlayerNameLength + 2) * 16, 720),
                BackgroundColor = new Color(0x123456ff)
            };

            _sidebarContainer = new Element(Desktop, this)
            {
                AnchorRectangle = new Rectangle(0, 0, Engine.Viewport.Width, Engine.Viewport.Height)
            };

            const int ButtonStripWidth = (128 + 2 * 8);
            const int SidebarPanelWidth = 400;

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

                new Button(Desktop, activeStrip)
                {
                    Text = label,
                    AnchorRectangle = new Rectangle(8, 8 + stripButtons * (buttonHeight + 8), activeStrip.AnchorRectangle.Width - 16, buttonHeight),
                    BackgroundColor = new Color(0x4444ccff),
                    OnActivate = action
                };

                stripButtons++;
            }

            // Manual mode
            _manualModeSidebar = new Element(Desktop, null)
            {
                AnchorRectangle = new Rectangle(Engine.Viewport.Width - ButtonStripWidth, 0, ButtonStripWidth, Engine.Viewport.Height),
            };

            StartButtonStrip(_manualModeSidebar);
            AddButtonToStrip("SCRIPT", () => Engine.SetupScriptForSelectedEntity(null));
            AddButtonToStrip("BUILD", () => Engine.PlanMove(Entity.EntityMove.Build));
            AddButtonToStrip("CW", () => Engine.PlanMove(Entity.EntityMove.RotateCW));
            AddButtonToStrip("MOVE", () => Engine.PlanMove(Entity.EntityMove.Move));
            AddButtonToStrip("CCW", () => Engine.PlanMove(Entity.EntityMove.RotateCCW));

            // Script selector
            _scriptSelectorSidebar = new Element(Desktop, null)
            {
                AnchorRectangle = new Rectangle(Engine.Viewport.Width - ButtonStripWidth - SidebarPanelWidth, 0, ButtonStripWidth + SidebarPanelWidth, Engine.Viewport.Height)
            };

            var scriptSelectorButtonStrip = StartButtonStrip(new Element(Desktop, _scriptSelectorSidebar)
            {
                AnchorRectangle = new Rectangle(0, 0, ButtonStripWidth, Engine.Viewport.Height)
            });

            AddButtonToStrip("MANUAL", () => Engine.ClearScriptForSelectedEntity());

            var scriptSelectorPanel = new Element(Desktop, _scriptSelectorSidebar)
            {
                AnchorRectangle = new Rectangle(ButtonStripWidth, 0, SidebarPanelWidth, Engine.Viewport.Height),
                BackgroundColor = new Color(0x123456ff)
            };

            new Button(Desktop, scriptSelectorPanel)
            {
                AnchorRectangle = new Rectangle(8, 8, SidebarPanelWidth - 16, 16),
                Text = "[+] New Script",
                OnActivate = () => Engine.CreateScriptForSelectedEntity()
            };

            _scriptSelectorList = new Element(Desktop, scriptSelectorPanel)
            {
                AnchorRectangle = new Rectangle(8, 32 + 8, SidebarPanelWidth - 16, Engine.Viewport.Height - 32 - 16)
            };

            // Script editor
            _scriptEditorSidebar = new Element(Desktop, null)
            {
                AnchorRectangle = new Rectangle(Engine.Viewport.Width - ButtonStripWidth - SidebarPanelWidth, 0, ButtonStripWidth + SidebarPanelWidth, Engine.Viewport.Height),
            };

            var scriptEditorButtonStrip = StartButtonStrip(new Element(Desktop, _scriptEditorSidebar)
            {
                AnchorRectangle = new Rectangle(0, 0, ButtonStripWidth, Engine.Viewport.Height)
            });

            AddButtonToStrip("STOP", () => Engine.SetupScriptForSelectedEntity(null));
            AddButtonToStrip("SAVE", () => { /* Engine.SaveScript() */ });

            var scriptEditorPanel = new Element(Desktop, _scriptEditorSidebar)
            {
                AnchorRectangle = new Rectangle(ButtonStripWidth, 0, SidebarPanelWidth, Engine.Viewport.Height),
                BackgroundColor = new Color(0x123456ff)
            };

            _scriptNameInput = new TextInput(Desktop, scriptEditorPanel)
            {
                AnchorRectangle = new Rectangle(8, 8, SidebarPanelWidth - 16, 16),
                BackgroundColor = new Color(0x004400ff),
                MaxLength = Protocol.MaxScriptNameLength
            };

            _scriptTextEditor = new TextEditor(Desktop, scriptEditorPanel)
            {
                AnchorRectangle = new Rectangle(8, 8 + 16 + 8, SidebarPanelWidth - 16, Engine.Viewport.Height - (8 + 16 + 8 + 8)),
                BackgroundColor = new Color(0x004400ff),
            };

            OnScriptListUpdated();
        }

        public override Element HitTest(int x, int y)
        {
            return base.HitTest(x, y) ?? (LayoutRectangle.Contains(x, y) ? this : null);
        }

        public override void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            if (repeat) return;

            if (key == SDL.SDL_Keycode.SDLK_TAB)
            {
                Add(_playerListPanel);
                _playerListPanel.Layout(LayoutRectangle);
            }

            if (key == SDL.SDL_Keycode.SDLK_a || key == SDL.SDL_Keycode.SDLK_q) Engine.IsScrollingLeft = true;
            if (key == SDL.SDL_Keycode.SDLK_d) Engine.IsScrollingRight = true;
            if (key == SDL.SDL_Keycode.SDLK_w || key == SDL.SDL_Keycode.SDLK_z) Engine.IsScrollingUp = true;
            if (key == SDL.SDL_Keycode.SDLK_s) Engine.IsScrollingDown = true;
        }

        public override void OnKeyUp(SDL.SDL_Keycode key)
        {
            if (key == SDL.SDL_Keycode.SDLK_TAB)
            {
                Remove(_playerListPanel);
            }

            if (key == SDL.SDL_Keycode.SDLK_a || key == SDL.SDL_Keycode.SDLK_q) Engine.IsScrollingLeft = false;
            if (key == SDL.SDL_Keycode.SDLK_d) Engine.IsScrollingRight = false;
            if (key == SDL.SDL_Keycode.SDLK_w || key == SDL.SDL_Keycode.SDLK_z) Engine.IsScrollingUp = false;
            if (key == SDL.SDL_Keycode.SDLK_s) Engine.IsScrollingDown = false;
        }

        public override void OnMouseMove()
        {
        }

        public override void OnMouseDown(int button)
        {
            if (button == 1)
            {
                foreach (var entity in Engine.Map.Entities)
                {
                    if (entity.X == Engine.HoveredTileX && entity.Y == Engine.HoveredTileY)
                    {
                        Engine.SetSelectedEntity(entity);
                        return;
                    }
                }

                Engine.SetSelectedEntity(null);
                return;
            }
        }

        public override void OnMouseUp(int button)
        {
        }

        public void OnPlayerListUpdated()
        {
            _playerListPanel.Clear();

            for (var i = 0; i < Engine.PlayerList.Count; i++)
            {
                var entry = Engine.PlayerList[i];
                var label = new Label(Desktop, _playerListPanel) { Text = $"[{entry.Team.ToString()}] {entry.Name}{(entry.IsOnline ? "" : " (offline)")}" };
                label.AnchorRectangle = new Rectangle(16, 16 + 16 * i, _playerListPanel.AnchorRectangle.Width, 16);
            }

            _playerListPanel.Layout(LayoutRectangle);
        }

        public void OnScriptListUpdated()
        {
            _scriptSelectorList.Clear();

            var i = 0;
            foreach (var scriptPath in Engine.Scripts.Keys)
            {
                new Button(Desktop, _scriptSelectorList)
                {
                    AnchorRectangle = new Rectangle(0, i * 16, _scriptSelectorList.AnchorRectangle.Width, 16),
                    Text = scriptPath,
                    OnActivate = () => Engine.SetupScriptForSelectedEntity(scriptPath)
                };

                i++;
            }

            _scriptSelectorList.Layout(_scriptSelectorList.Parent.LayoutRectangle);
        }

        public void OnSelectedEntityChanged()
        {
            _sidebarContainer.Clear();
            if (Engine.SelectedEntity == null) return;

            if (Engine.EntityScripts.TryGetValue(Engine.SelectedEntity.Id, out var scriptPath))
            {
                if (scriptPath == null)
                {
                    _sidebarContainer.Add(_scriptSelectorSidebar);
                }
                else
                {
                    _sidebarContainer.Add(_scriptEditorSidebar);
                    _scriptNameInput.Value = scriptPath;
                    _scriptTextEditor.SetText(Engine.Scripts[scriptPath]);
                    Desktop.FocusedElement = _scriptTextEditor;
                }
            }
            else
            {
                _sidebarContainer.Add(_manualModeSidebar);
            }

            _sidebarContainer.Layout(Engine.Viewport);
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            DrawMap();
        }

        void DrawMap()
        {
            var startTileX = (int)Engine.ScrollingPixelsX / Map.TileSize;
            var startTileY = (int)Engine.ScrollingPixelsY / Map.TileSize;

            var tilesPerRow = (int)MathF.Ceiling((float)Engine.Viewport.Width / Map.TileSize + 1);
            var tilesPerColumn = (int)MathF.Ceiling((float)Engine.Viewport.Height / Map.TileSize + 1);

            var tileViewport = new Rectangle(startTileX, startTileY, tilesPerRow, tilesPerColumn);

            // Tiles
            for (var y = 0; y < tilesPerColumn; y++)
            {
                for (var x = 0; x < tilesPerRow; x++)
                {
                    var index = (startTileY + y) * Map.MapSize + (startTileX + x);

                    var rect = new SDL.SDL_Rect
                    {
                        x = (startTileX + x) * Map.TileSize - (int)Engine.ScrollingPixelsX,
                        y = (startTileY + y) * Map.TileSize - (int)Engine.ScrollingPixelsY,
                        w = Map.TileSize,
                        h = Map.TileSize
                    };

                    var color = new Color(Map.TileColors[(int)Engine.Map.Tiles[index]]);
                    color.UseAsDrawColor(Engine.Renderer);

                    SDL.SDL_RenderFillRect(Engine.Renderer, ref rect);
                }
            }

            // Fog of war
            var fogColor = new Color(0x00000044);
            fogColor.UseAsDrawColor(Engine.Renderer);
            SDL.SDL_SetRenderDrawBlendMode(Engine.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

            for (var y = 0; y < tilesPerColumn; y++)
            {
                for (var x = 0; x < tilesPerRow; x++)
                {
                    var index = (startTileY + y) * Map.MapSize + (startTileX + x);
                    if (Engine.FogOfWar[index] != 0) continue;

                    var rect = new SDL.SDL_Rect
                    {
                        x = (startTileX + x) * Map.TileSize - (int)Engine.ScrollingPixelsX,
                        y = (startTileY + y) * Map.TileSize - (int)Engine.ScrollingPixelsY,
                        w = Map.TileSize,
                        h = Map.TileSize
                    };

                    SDL.SDL_RenderFillRect(Engine.Renderer, ref rect);
                }
            }

            SDL.SDL_SetRenderDrawBlendMode(Engine.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);

            // Entities
            foreach (var entity in Engine.Map.Entities)
            {
                if (!tileViewport.Contains(entity.X, entity.Y)) continue;

                var x = entity.X * Map.TileSize - (int)Engine.ScrollingPixelsX;
                var y = entity.Y * Map.TileSize - (int)Engine.ScrollingPixelsY;

                switch (entity.Type)
                {
                    case Entity.EntityType.Factory:
                        {
                            var sourceRect = Desktop.ToSDL_Rect(new Rectangle(0, 0, 24 * 3, 24 * 3));
                            var destRect = Desktop.ToSDL_Rect(new Rectangle(x - 24, y - 24, 24 * 3, 24 * 3));
                            SDL.SDL_RenderCopy(Engine.Renderer, Engine.SpritesheetTexture, ref sourceRect, ref destRect);
                            break;
                        }

                    case Entity.EntityType.Heart:
                        {
                            var teamOffset = Engine.PlayerList[entity.PlayerIndex].Team == Player.PlayerTeam.Blue ? 0 : 1;

                            var sourceRect = Desktop.ToSDL_Rect(new Rectangle(24 * (3 + teamOffset), 0, 24, 24));
                            var destRect = Desktop.ToSDL_Rect(new Rectangle(x, y, 24, 24));
                            SDL.SDL_RenderCopy(Engine.Renderer, Engine.SpritesheetTexture, ref sourceRect, ref destRect);
                            break;
                        }

                    case Entity.EntityType.Robot:
                        {
                            var teamOffset = Engine.PlayerList[entity.PlayerIndex].Team == Player.PlayerTeam.Blue ? 0 : 1;

                            SDL.SDL_Rect sourceRect;
                            SDL.SDL_Rect destRect;

                            switch (entity.Direction)
                            {
                                case Entity.EntityDirection.Left:
                                    sourceRect = Desktop.ToSDL_Rect(new Rectangle(0, 24 * (3 + teamOffset * 3), 24 * 2, 24 * 3));
                                    destRect = Desktop.ToSDL_Rect(new Rectangle(x - 24, y - 24, 24 * 2, 24 * 3));
                                    break;

                                case Entity.EntityDirection.Down:
                                    sourceRect = Desktop.ToSDL_Rect(new Rectangle(24 * 2, 24 * (3 + teamOffset * 3), 24, 24 * 3));
                                    destRect = Desktop.ToSDL_Rect(new Rectangle(x, y - 24, 24, 24 * 3));
                                    break;

                                case Entity.EntityDirection.Up:
                                    sourceRect = Desktop.ToSDL_Rect(new Rectangle(24 * 3, 24 * (3 + teamOffset * 3), 24, 24 * 3));
                                    destRect = Desktop.ToSDL_Rect(new Rectangle(x, y - 24, 24, 24 * 3));
                                    break;

                                case Entity.EntityDirection.Right:
                                    sourceRect = Desktop.ToSDL_Rect(new Rectangle(24 * 4, 24 * (3 + teamOffset * 3), 24 * 2, 24 * 3));
                                    destRect = Desktop.ToSDL_Rect(new Rectangle(x, y - 24, 24 * 2, 24 * 3));
                                    break;

                                default: throw new NotSupportedException();
                            }

                            SDL.SDL_RenderCopy(Engine.Renderer, Engine.SpritesheetTexture, ref sourceRect, ref destRect);
                            break;
                        }

                    default:
                        {
                            var stats = Entity.EntityStatsByType[(int)entity.Type];
                            var color = new Color(stats.NeutralColor);
                            if (entity.PlayerIndex != -1) color.RGBA = Engine.PlayerList[entity.PlayerIndex].Team == Player.PlayerTeam.Blue ? stats.BlueColor : stats.RedColor;

                            color.UseAsDrawColor(Engine.Renderer);

                            var rect = Desktop.ToSDL_Rect(new Rectangle(x, y, Map.TileSize, Map.TileSize));
                            SDL.SDL_RenderFillRect(Engine.Renderer, ref rect);
                            break;
                        }
                }

            }

            if (Engine.SelectedEntity != null)
            {
                var color = new Color(0x00ff00ff);
                color.UseAsDrawColor(Engine.Renderer);

                var x = Engine.SelectedEntity.X;
                var y = Engine.SelectedEntity.Y;
                var w = 1;
                var h = 1;

                switch (Engine.SelectedEntity.Type)
                {
                    case Entity.EntityType.Factory: x -= 1; y -= 1; w = 3; h = 2; break;
                }

                var rect = new Rectangle(
                    x * Map.TileSize - (int)Engine.ScrollingPixelsX,
                    y * Map.TileSize - (int)Engine.ScrollingPixelsY,
                    w * Map.TileSize, h * Map.TileSize);

                SDL.SDL_RenderDrawLine(Engine.Renderer, rect.X, rect.Y, rect.X + rect.Width, rect.Y);
                SDL.SDL_RenderDrawLine(Engine.Renderer, rect.X + rect.Width, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Engine.Renderer, rect.X + rect.Width, rect.Y + rect.Height, rect.X, rect.Y + rect.Height);
                SDL.SDL_RenderDrawLine(Engine.Renderer, rect.X, rect.Y + rect.Height, rect.X, rect.Y);
            }
        }
    }
}
