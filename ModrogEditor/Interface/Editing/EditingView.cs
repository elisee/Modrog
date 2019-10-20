using ModrogEditor.Scenario;
using SwarmCore;
using SwarmPlatform.Graphics;
using SwarmPlatform.Interface;
using SwarmPlatform.UI;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ModrogEditor.Interface.Editing
{
    class EditingView : EditorElement
    {
        readonly Panel _sidebarPanel;
        readonly AssetTree _assetTree;

        readonly Panel _mainPanel;
        readonly Element _editorContainer;
        readonly Label _assetTitleLabel;

        public EditingView(EditorApp @interface)
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
                OnActivate = (entry) => App.State.OpenAsset(entry)
            };

            _mainPanel = new Panel(this)
            {
                LayoutWeight = 1,
                ChildLayout = ChildLayoutMode.Top,
                BackgroundPatch = new TexturePatch(0x000123ff)
            };

            var topBar = new Panel(_mainPanel)
            {
                ChildLayout = ChildLayoutMode.Left,
                BackgroundPatch = new TexturePatch(0x654321ff),
            };

            var tabsBar = new Element(topBar)
            {
                LayoutWeight = 1,
                ChildLayout = ChildLayoutMode.Left
            };

            _assetTitleLabel = new Label(tabsBar) { Flow = Flow.Shrink, Padding = 8 };

            new StyledTextButton(topBar)
            {
                Text = "Run",
                OnActivate = () =>
                {
                    var clientExePath = Path.Combine(FileHelper.FindAppFolder("ModrogClient-Debug"), "netcoreapp3.0", "ModrogClient.exe");
                    Process.Start(clientExePath, "--scenario " + App.State.ActiveScenarioEntry.Name);
                }
            };

            _editorContainer = new Element(_mainPanel) { LayoutWeight = 1 };
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

            MakeAssetEntries(null, App.State.AssetEntries);

            Desktop.SetFocusedElement(this);
        }

        public void OnActiveAssetChanged()
        {
            _editorContainer.Clear();

            var entry = App.State.ActiveAssetEntry;
            var fullAssetPath = Path.Combine(App.State.ActiveScenarioPath, entry.Path);
            Element editor = null;
            _assetTitleLabel.Text = entry.Path;
            _assetTitleLabel.Parent.Layout();

            switch (entry.AssetType)
            {
                case AssetType.Unknown:
                case AssetType.Folder:
                    break;

                case AssetType.Manifest: editor = new Manifest.ManifestEditor(App, fullAssetPath); break;
                case AssetType.TileSet: editor = new TileSet.TileSetEditor(App, fullAssetPath); break;
                case AssetType.Script: editor = new Script.ScriptEditor(App, fullAssetPath); break;
                case AssetType.Image: editor = new Image.ImageEditor(App, fullAssetPath); break;
                case AssetType.Map: editor = new Map.MapEditor(App, fullAssetPath); break;
            }

            if (editor != null) _editorContainer.Add(editor);
            _editorContainer.Layout();
        }
    }
}
