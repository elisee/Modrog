﻿using ModrogCommon;
using SwarmCore;
using SwarmPlatform.Graphics;
using SwarmPlatform.Interface;
using SwarmPlatform.UI;
using System;
using System.IO;
using System.Text.Json;

namespace ModrogEditor.Interface.Editing.Map
{
    class MapEditor : BaseAssetEditor
    {
        readonly Panel _mainLayer;
        readonly MapViewport _mapViewport;
        readonly TileSelector _tileSelector;

        readonly MapSettingsLayer _mapSettingsLayer;

        readonly Panel _errorLayer;
        readonly Label _errorTitleLabel;
        readonly Label _errorDetailsLabel;

        // Tile Kinds
        public string TileSetPath = "";

        // Tools
        public enum MapEditorTool { Brush, Picker, Bucket }
        public MapEditorTool Tool { get; private set; } = MapEditorTool.Brush;

        public int TileLayer = 0;
        public short BrushTileIndex = 1;
        public bool BrushShouldErase = false;
        public int BrushSize = 1;


        public MapEditor(Interface @interface, string fullAssetPath)
            : base(@interface, fullAssetPath)
        {
            // Main layer
            {
                _mainLayer = new Panel(this)
                {
                    ChildLayout = ChildLayoutMode.Top
                };

                var topBar = new Panel(_mainLayer)
                {
                    BackgroundPatch = new TexturePatch(0x123456ff),
                    ChildLayout = ChildLayoutMode.Left,
                    VerticalPadding = 8
                };

                new StyledTextButton(topBar)
                {
                    Text = "Save",
                    Right = 8,
                    OnActivate = () => Save()
                };

                new StyledTextButton(topBar)
                {
                    Text = "Settings",
                    OnActivate = () =>
                    {
                        _mapSettingsLayer.Visible = true;
                        _mapSettingsLayer.Layout(_contentRectangle);
                    }
                };

                {
                    var splitter = new Element(_mainLayer) { LayoutWeight = 1, ChildLayout = ChildLayoutMode.Left };

                    _mapViewport = new MapViewport(this, splitter) { LayoutWeight = 1 };

                    var sidebar = new Panel(splitter) { Width = 300, ChildLayout = ChildLayoutMode.Top, Left = 8 };

                    // Layers
                    var layersPanel = new Panel(sidebar) { ChildLayout = ChildLayoutMode.Top };
                    new Label(layersPanel) { Text = "LAYERS", Padding = 8, BackgroundPatch = new TexturePatch(0x456456ff) };

                    // Tools
                    var toolsPanel = new Panel(sidebar) { ChildLayout = ChildLayoutMode.Top };
                    new Label(toolsPanel) { Text = "TOOLS", Padding = 8, BackgroundPatch = new TexturePatch(0x456456ff) };

                    var toolStrip = new Element(toolsPanel) { ChildLayout = ChildLayoutMode.Left };

                    StyledTextButton MakeButton(string name, Action onActivate)
                    {
                        return new StyledTextButton(toolStrip) { LayoutWeight = 1, HorizontalFlow = Flow.Expand, Text = name, OnActivate = onActivate };
                    }

                    MakeButton("Brush", () => SetBrush(tileIndex: 1));
                    MakeButton("Picker", () => Tool = MapEditorTool.Picker);
                    MakeButton("Bucket", () => Tool = MapEditorTool.Bucket);

                    // Tile set
                    var tileSetPanel = new Panel(sidebar) { ChildLayout = ChildLayoutMode.Top, LayoutWeight = 1 };
                    new Label(tileSetPanel) { Text = "TILE SET", Padding = 8, BackgroundPatch = new TexturePatch(0x456456ff) };

                    _tileSelector = new TileSelector(this, tileSetPanel) { LayoutWeight = 1 };
                }
            }

            // Settings popup
            _mapSettingsLayer = new MapSettingsLayer(this) { Visible = false };

            // Error popup
            {
                _errorLayer = new Panel(this) { Visible = false, BackgroundPatch = new TexturePatch(0x00000088) };

                var windowPanel = new Panel(_errorLayer)
                {
                    BackgroundPatch = new TexturePatch(0x228800ff),
                    Width = 480,
                    Flow = Flow.Shrink,
                    ChildLayout = ChildLayoutMode.Top,
                };

                var titlePanel = new Panel(windowPanel, new TexturePatch(0x88aa88ff));
                _errorTitleLabel = new Label(titlePanel) { Flow = Flow.Shrink, Padding = 8 };

                _errorDetailsLabel = new Label(windowPanel)
                {
                    Wrap = true,
                    Padding = 8,
                };
            }
        }

        public override void OnMounted()
        {
            void OnError(string details)
            {
                _errorTitleLabel.Text = "Cannot open map";
                _errorDetailsLabel.Text = details;
                _errorLayer.Visible = true;
                Desktop.SetFocusedElement(_errorLayer);
            }

            {
                try
                {
                    // TODO: This should move into a map asset
                    var mapReader = new PacketReader();
                    mapReader.Open(File.ReadAllBytes(FullAssetPath));
                    TileSetPath = mapReader.ReadByteSizeString();
                }
                catch (Exception exception)
                {
                    OnError("Error while loading map: " + exception.Message);
                    return;
                }
            }

            {
                JsonElement tileSetJson;
                try
                {
                    // TODO: This should move into a tileset asset
                    tileSetJson = JsonHelper.Parse(File.ReadAllText(Path.Combine(Engine.State.ActiveScenarioPath, TileSetPath)));

                    // TODO: Parse tileset and 
                }
                catch (Exception exception)
                {
                    OnError("Error while loading tileset: " + exception.Message);
                    return;
                }
            }

            Desktop.SetFocusedElement(_mapViewport);
        }

        public override void OnUnmounted()
        {
            Save();
        }

        void Save()
        {
            var writer = new PacketWriter(capacity: 8192, useSizeHeader: false);
            writer.WriteByteSizeString(TileSetPath);

            using var file = File.OpenWrite(FullAssetPath);
            file.Write(writer.Buffer, 0, writer.Finish());
        }

        internal void SetBrush(short tileIndex)
        {
            Tool = MapEditorTool.Brush;
            BrushTileIndex = tileIndex;
        }

        internal void CloseSettings()
        {
            _mapSettingsLayer.Visible = false;

            Desktop.SetFocusedElement(_mapViewport);
        }
    }
}