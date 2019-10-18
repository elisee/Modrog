using DeepSwarmCommon;
using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.Interface;
using DeepSwarmPlatform.UI;
using System.IO;

namespace DeepSwarmScenarioEditor.Interface.Editing.Map
{
    class MapEditor : BaseAssetEditor
    {
        readonly MapSettingsLayer _mapSettingsLayer;

        public string TileSetPath = "";

        public MapEditor(Interface @interface, string fullAssetPath)
            : base(@interface, fullAssetPath)
        {
            {
                var mainLayer = new Panel(this)
                {
                    ChildLayout = ChildLayoutMode.Top
                };

                var topBar = new Panel(mainLayer)
                {
                    BackgroundPatch = new TexturePatch(0x123456ff),
                    ChildLayout = ChildLayoutMode.Left,
                    VerticalPadding = 8
                };

                new StyledTextButton(topBar)
                {
                    Text = "Save",
                    Right = 8,
                    OnActivate = () =>
                    {
                        Save();
                    }
                };

                new StyledTextButton(topBar)
                {
                    Text = "Settings",
                    OnActivate = () =>
                    {
                        _mapSettingsLayer.Visible = true;
                        _mapSettingsLayer.Layout(_contentRectangle);
                        Desktop.SetFocusedElement(_mapSettingsLayer);
                    }
                };

                var viewport = new Panel(mainLayer)
                {
                    LayoutWeight = 1
                };
            }

            _mapSettingsLayer = new MapSettingsLayer(this) { Visible = false };
        }

        public override void OnMounted()
        {
            var reader = new PacketReader();
            reader.Open(File.ReadAllBytes(FullAssetPath));

            TileSetPath = reader.ReadByteSizeString();
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
    }
}
