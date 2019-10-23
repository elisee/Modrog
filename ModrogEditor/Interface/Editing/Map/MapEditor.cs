using ModrogCommon;
using SDL2;
using SwarmBasics.Math;
using SwarmCore;
using SwarmPlatform.Graphics;
using SwarmPlatform.Interface;
using SwarmPlatform.UI;
using System;
using System.IO;
using System.Runtime.InteropServices;
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

        // Spritesheet
        public string SpritesheetPath { get; private set; }
        public IntPtr SpritesheetTexture { get; private set; }

        // Tile Kinds
        public string TileSetPath = "";

        public struct EditorTileKind
        {
            public string Name;
            public Point SpriteLocation;
        }

        public readonly EditorTileKind[][] TileKindsByLayer = new EditorTileKind[(int)ModrogApi.MapLayer.Count][];

        TextButton[] _tileLayerButtons = new TextButton[(int)ModrogApi.MapLayer.Count];

        // Tools
        public enum MapEditorTool { Brush, Picker, Bucket }
        public MapEditorTool Tool { get; private set; } = MapEditorTool.Brush;

        public int TileLayer = 0;
        public short BrushTileIndex = 1;
        public bool BrushShouldErase = false;
        public int BrushSize = 1;

        public MapEditor(EditorApp @interface, string fullAssetPath)
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

                    void SetTileLayer(int layer)
                    {
                        _tileLayerButtons[TileLayer].BackgroundPatch = new TexturePatch(0x00000000);
                        _tileLayerButtons[layer].BackgroundPatch = new TexturePatch(0x228822ff);

                        TileLayer = layer;
                    }

                    for (var i = 0; i < (int)ModrogApi.MapLayer.Count; i++)
                    {
                        var layer = i;

                        _tileLayerButtons[i] = new TextButton(layersPanel)
                        {
                            Text = Enum.GetName(typeof(ModrogApi.MapLayer), i),
                            Padding = 8,
                            OnActivate = () => SetTileLayer(layer)
                        };
                    }

                    SetTileLayer(0);

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
                _mainLayer.Visible = false;

                _errorTitleLabel.Text = "Cannot open map";
                _errorDetailsLabel.Text = details;
                _errorLayer.Visible = true;
                Desktop.SetFocusedElement(_errorLayer);
            }

            {
                try
                {
                    var reader = new PacketReader();
                    reader.Open(File.ReadAllBytes(FullAssetPath));
                    TileSetPath = reader.ReadByteSizeString();

                    var chunksCount = reader.ReadInt();

                    for (var i = 0; i < chunksCount; i++)
                    {
                        var coords = new Point(reader.ReadInt(), reader.ReadInt());
                        var tilesPerLayer = new short[(int)ModrogApi.MapLayer.Count][];

                        for (var j = 0; j < (int)ModrogApi.MapLayer.Count; j++)
                        {
                            tilesPerLayer[j] = MemoryMarshal.Cast<byte, short>(reader.ReadBytes(Protocol.MapChunkSide * Protocol.MapChunkSide * sizeof(short))).ToArray();
                        }

                        var chunk = new Chunk(tilesPerLayer);
                        _mapViewport.Chunks.Add(coords, chunk);
                    }
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
                    tileSetJson = JsonHelper.Parse(File.ReadAllText(Path.Combine(App.State.ActiveScenarioPath, TileSetPath)));

                    SpritesheetPath = tileSetJson.GetProperty("spritesheet").GetString();

                    var tileKindsJson = tileSetJson.GetProperty("tileKinds");

                    for (var i = 0; i < (int)ModrogApi.MapLayer.Count; i++)
                    {
                        var layerName = Enum.GetName(typeof(ModrogApi.MapLayer), i);

                        if (tileKindsJson.TryGetProperty(layerName, out var layerJson))
                        {
                            var tileKinds = TileKindsByLayer[i] = new EditorTileKind[layerJson.GetArrayLength()];

                            for (var j = 0; j < tileKinds.Length; j++)
                            {
                                var tileKindJson = layerJson[j];

                                var name = tileKindJson.GetProperty("name").GetString();

                                var spriteLocationJson = tileKindJson.GetProperty("spriteLocation");
                                var spriteLocation = new Point(
                                    spriteLocationJson[0].GetInt32(),
                                    spriteLocationJson[1].GetInt32());

                                tileKinds[j] = new EditorTileKind { Name = name, SpriteLocation = spriteLocation };
                            }
                        }
                        else
                        {
                            TileKindsByLayer[i] = new EditorTileKind[0];
                        }
                    }

                }
                catch (Exception exception)
                {
                    OnError("Error while loading tile set: " + exception.Message);
                    return;
                }
            }

            {
                try
                {
                    SpritesheetTexture = SDL_image.IMG_LoadTexture(Desktop.Renderer, Path.Combine(App.State.ActiveScenarioPath, SpritesheetPath));
                    if (SpritesheetTexture == IntPtr.Zero) throw new Exception(SDL.SDL_GetError());
                }
                catch (Exception exception)
                {
                    OnError("Error while loading spritesheet texture: " + exception.Message);
                    return;
                }
            }

            _mapViewport.Visible = true;
            Desktop.SetFocusedElement(_mapViewport);
        }

        public override void OnUnmounted()
        {
            if (SpritesheetTexture != IntPtr.Zero)
            {
                SDL.SDL_DestroyTexture(SpritesheetTexture);
                SpritesheetTexture = IntPtr.Zero;
            }

            Save();
        }

        void Save()
        {
            var writer = new PacketWriter(initialCapacity: 8192, useSizeHeader: false);
            writer.WriteByteSizeString(TileSetPath);
            writer.WriteInt(_mapViewport.Chunks.Count);

            foreach (var (coords, chunk) in _mapViewport.Chunks)
            {
                writer.WriteInt(coords.X);
                writer.WriteInt(coords.Y);
                for (var i = 0; i < (int)ModrogApi.MapLayer.Count; i++) writer.WriteShorts(chunk.TilesPerLayer[i]);
            }

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
