using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.UI;
using DeepSwarmScenarioEditor.Scenario;
using System.Collections.Generic;

namespace DeepSwarmScenarioEditor.Interface.Editing
{
    class EditingView : InterfaceElement
    {
        readonly Panel _sidebarPanel;
        readonly AssetTree _assetTree;

        readonly Panel _mainPanel;

        public EditingView(Interface @interface)
            : base(@interface, null)
        {
            ChildLayout = ChildLayoutMode.Left;

            _sidebarPanel = new Panel(this)
            {
                BackgroundPatch = new TexturePatch(0x123456ff),
                Padding = 8,
                Width = 300,
                ChildLayout = ChildLayoutMode.Top,
            };

            new Label(_sidebarPanel)
            {
                Text = "ASSETS",
                Bottom = 8,
            };

            _assetTree = new AssetTree(_sidebarPanel)
            {
                BackgroundPatch = new TexturePatch(0x001234ff),
                LayoutWeight = 1,
                VerticalFlow = Flow.Scroll,
                Padding = 8,
                OnActivate = (entry) => Engine.State.OpenAsset(entry)
            };

            _mainPanel = new Panel(this)
            {
                LayoutWeight = 1,
                BackgroundPatch = new TexturePatch(0x000123ff)
            };
        }

        public override void OnMounted()
        {
            _assetTree.Clear();

            void MakeAssetEntries(AssetTreeItem parent, List<AssetEntry> entries)
            {
                foreach (var entry in entries)
                {
                    var item = new AssetTreeItem(_assetTree, entry);
                    if (parent != null) parent.AddChildItem(item);
                    else _assetTree.Add(item);

                    if (entry.Children.Count > 0) MakeAssetEntries(item, entry.Children);
                }
            }

            MakeAssetEntries(null, Engine.State.AssetEntries);

            Desktop.SetFocusedElement(this);
        }

        public void OnActiveAssetChanged()
        {
            _mainPanel.Clear();

            var entry = Engine.State.ActiveAssetEntry;
            Element editor = null;

            switch (entry.AssetType)
            {
                case AssetType.Unknown:
                case AssetType.Folder:
                    break;

                case AssetType.Manifest: editor = new Manifest.ManifestEditor(Engine.Interface, _mainPanel); break;
                case AssetType.TileSet: editor = new TileSet.TileSetEditor(Engine.Interface, _mainPanel); break;
                case AssetType.Script: editor = new Script.ScriptEditor(Engine.Interface, _mainPanel); break;
                case AssetType.Image: editor = new Image.ImageEditor(Engine.Interface, _mainPanel); break;
                case AssetType.Map: editor = new Map.MapEditor(Engine.Interface, _mainPanel); break;
            }

            if (editor != null) Desktop.SetFocusedElement(editor);
            _mainPanel.Layout();
        }
    }
}
