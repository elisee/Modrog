using DeepSwarmCommon;
using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.Interface;
using DeepSwarmPlatform.UI;
using System.IO;

namespace DeepSwarmScenarioEditor.Interface.Editing.Map
{
    class MapEditor : BaseAssetEditor
    {
        readonly Panel _mainLayer;
        readonly MapViewport _mapViewport;

        readonly MapSettingsLayer _mapSettingsLayer;

        // Tile Kinds
        public string TileSetPath = "";

        public MapEditor(Interface @interface, string fullAssetPath)
            : base(@interface, fullAssetPath)
        {
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
                        _mainLayer.Disabled = true;
                        _mapSettingsLayer.Visible = true;
                        _mapSettingsLayer.Layout(_contentRectangle);
                    }
                };

                _mapViewport = new MapViewport(this) { LayoutWeight = 1 };
                _mainLayer.Add(_mapViewport);
            }

            _mapSettingsLayer = new MapSettingsLayer(this) { Visible = false };
        }

        public override void OnMounted()
        {
            var reader = new PacketReader();
            reader.Open(File.ReadAllBytes(FullAssetPath));

            TileSetPath = reader.ReadByteSizeString();

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

        internal void CloseSettings()
        {
            _mapSettingsLayer.Visible = false;
            _mainLayer.Disabled = false;

            Desktop.SetFocusedElement(_mapViewport);
        }
    }
}
