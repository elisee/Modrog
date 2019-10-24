using ModrogEditor.Scenario;
using SwarmCore;
using SwarmPlatform.Graphics;
using SwarmPlatform.Interface;
using SwarmPlatform.UI;
using System.Diagnostics;
using System.IO;

namespace ModrogEditor.Interface.Editing
{
    class EditingView : EditorElement
    {
        readonly Panel _sidebarPanel;
        readonly AssetTree _assetTree;

        readonly StyledTextButton _newAssetButton;
        readonly NewAssetLayer _newAssetLayer;

        readonly Panel _mainPanel;
        readonly Element _editorContainer;
        readonly Label _assetTitleLabel;

        public EditingView(EditorApp @interface)
            : base(@interface, null)
        {
            var rootContainer = new Element(this)
            {
                ChildLayout = ChildLayoutMode.Left
            };

            _sidebarPanel = new Panel(rootContainer)
            {
                BackgroundPatch = new TexturePatch(0x123456ff),
                Padding = 8,
                Width = 300,
                ChildLayout = ChildLayoutMode.Top,
            };

            var headerBar = new Element(_sidebarPanel)
            {
                ChildLayout = ChildLayoutMode.Left,
                Bottom = 8,
            };

            new Label(headerBar)
            {
                Text = "ASSETS",
                LayoutWeight = 1,
            };

            _newAssetButton = new StyledTextButton(headerBar)
            {
                Text = "New",
                OnActivate = () =>
                {
                    _newAssetLayer.Visible = true;
                    _newAssetLayer.Layout(_contentRectangle);
                }
            };

            _newAssetLayer = new NewAssetLayer(this) { Visible = false };

            _assetTree = new AssetTree(_sidebarPanel)
            {
                BackgroundPatch = new TexturePatch(0x001234ff),
                LayoutWeight = 1,
                VerticalFlow = Flow.Scroll,
                Padding = 8,
                OnActivate = (entry) => App.State.OpenAsset(entry)
            };

            _mainPanel = new Panel(rootContainer)
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

            void MakeAssetChildrenEntries(AssetEntry parentEntry)
            {
                foreach (var entry in parentEntry.Children)
                {
                    _assetTree.AddEntry(entry);
                    if (entry.Children.Count > 0) MakeAssetChildrenEntries(entry);
                }
            }

            MakeAssetChildrenEntries(App.State.RootAssetEntry);

            Desktop.SetFocusedElement(this);
        }

        public void FocusNewAssetButton()
        {
            Desktop.SetFocusedElement(_newAssetButton);
        }

        public void CloseNewAssetLayer()
        {
            _newAssetLayer.Visible = false;
        }

        public void OnAssetCreated(AssetEntry entry)
        {
            _assetTree.AddEntry(entry);
            _assetTree.ShowEntry(entry);
            _assetTree.Layout();
        }

        public void OnActiveAssetChanged()
        {
            var entry = App.State.ActiveAssetEntry;

            _assetTree.SetSelectedEntry(entry);

            if (entry.AssetType == AssetType.Unknown || entry.AssetType == AssetType.Folder)
            {
                Desktop.SetFocusedElement(_editorContainer);
                return;
            }

            var fullAssetPath = Path.Combine(App.State.ActiveScenarioPath, entry.Path);
            Element editor = null;
            _assetTitleLabel.Text = entry.Path;
            _assetTitleLabel.Parent.Layout();

            switch (entry.AssetType)
            {
                case AssetType.Manifest: editor = new Manifest.ManifestEditor(App, fullAssetPath); break;
                case AssetType.TileSet: editor = new TileSet.TileSetEditor(App, fullAssetPath); break;
                case AssetType.Script: editor = new Script.ScriptEditor(App, fullAssetPath); break;
                case AssetType.Image: editor = new Image.ImageEditor(App, fullAssetPath); break;
                case AssetType.Map: editor = new Map.MapEditor(App, fullAssetPath); break;
            }

            _editorContainer.Clear();
            _editorContainer.Add(editor);
            _editorContainer.Layout();
        }
    }
}
