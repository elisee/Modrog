using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.UI;
using DeepSwarmScenarioEditor.Scenario;
using System.Collections.Generic;
using System.IO;

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

            void MakeAssetEntries(Element parent, List<AssetEntry> entries)
            {
                foreach (var entry in entries)
                {
                    var item = new AssetTreeItem(parent, _assetTree, entry);
                    if (entry.Children.Count > 0) MakeAssetEntries(item.ChildrenPanel, entry.Children);
                }
            }

            MakeAssetEntries(_assetTree, Engine.State.AssetEntries);

            Desktop.SetFocusedElement(this);
        }

        public void OnActiveAssetChanged()
        {
            _mainPanel.Clear();

            var entry = Engine.State.ActiveAssetEntry;
            var fullPath = Path.Combine(Engine.State.ScenariosPath, Engine.State.ActiveScenarioEntry.Name, entry.Path);

            switch (entry.AssetType)
            {
                case AssetType.Unknown:
                case AssetType.Folder:
                    break;

                case AssetType.Manifest:
                    {
                        var editor = new ManifestEditor(Engine.Interface, _mainPanel);
                        Desktop.SetFocusedElement(editor);
                        break;
                    }

                case AssetType.TileSet:
                case AssetType.Script:
                    {
                        var editor = new TextEditor(_mainPanel) { Padding = 8 };
                        editor.SetText(File.ReadAllText(fullPath));
                        Desktop.SetFocusedElement(editor);
                        break;
                    }

                case AssetType.Image:
                    {
                        var editor = new ImageEditor(Engine.Interface, _mainPanel, fullPath);
                        Desktop.SetFocusedElement(editor);
                        break;
                    }

                case AssetType.Map:
                    break;
            }

            _mainPanel.Layout();
        }
    }
}
