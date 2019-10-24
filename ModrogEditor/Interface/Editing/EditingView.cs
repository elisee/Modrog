using ModrogEditor.Scenario;
using SwarmCore;
using SwarmPlatform.Graphics;
using SwarmPlatform.Interface;
using SwarmPlatform.UI;
using System;
using System.Collections.Generic;
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
        readonly DeleteAssetLayer _deleteAssetLayer;

        readonly Panel _mainPanel;
        readonly Element _tabsBar;
        public Element _activeEditorContainer;

        class OpenAssetUI { public Element Tab; public BaseAssetEditor Editor; }
        readonly Dictionary<AssetEntry, OpenAssetUI> _openAssetUIsByEntry = new Dictionary<AssetEntry, OpenAssetUI>();

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
                VerticalFlow = Flow.Shrink
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
            _deleteAssetLayer = new DeleteAssetLayer(this) { Visible = false };

            _assetTree = new AssetTree(_sidebarPanel)
            {
                BackgroundPatch = new TexturePatch(0x001234ff),
                LayoutWeight = 1,
                VerticalFlow = Flow.Scroll,
                Padding = 8,
                OnActivate = (entry) =>
                {
                    if (entry.AssetType != AssetType.Folder && entry.AssetType != AssetType.Unknown) OpenAsset(entry);
                },
                OnDeleteSelectedAsset = (entry) =>
                 {
                     _deleteAssetLayer.SetSelectedEntry(entry);
                     _deleteAssetLayer.Visible = true;
                     _deleteAssetLayer.Layout(_contentRectangle);
                 }
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

            _tabsBar = new Element(topBar)
            {
                LayoutWeight = 1,
                ChildLayout = ChildLayoutMode.Left
            };

            new StyledTextButton(topBar)
            {
                Text = "Run",
                OnActivate = () =>
                {
                    var clientExePath = Path.Combine(FileHelper.FindAppFolder("ModrogClient-Debug"), "netcoreapp3.0", "ModrogClient.exe");
                    Process.Start(clientExePath, "--scenario " + App.State.ActiveScenarioEntry.Name);
                }
            };

            _activeEditorContainer = new Element(_mainPanel) { LayoutWeight = 1 };
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

        internal AssetEntry GetSelectedAssetTreeFolderEntry()
        {
            var entry = _assetTree.GetSelectedEntry() ?? App.State.RootAssetEntry;
            return entry.AssetType == AssetType.Folder ? entry : entry.Parent;
        }

        internal void OpenAsset(AssetEntry entry)
        {
            if (!_openAssetUIsByEntry.TryGetValue(entry, out var assetUI))
            {
                var tab = new Button(_tabsBar)
                {
                    HorizontalFlow = Flow.Shrink,
                    ChildLayout = ChildLayoutMode.Left,
                    BackgroundPatch = new TexturePatch(0x226622ff),
                    Padding = 8,
                    Right = 8,
                    OnActivate = () => OpenAsset(entry)
                };

                new Label(tab) { Flow = Flow.Shrink, Text = entry.Path };
                new TextButton(tab) { Left = 8, Text = "(x)", OnActivate = () => CloseAsset(entry) };

                _tabsBar.Layout();

                var fullAssetPath = Path.Combine(App.State.ActiveScenarioPath, entry.Path);
                BaseAssetEditor editor;

                switch (entry.AssetType)
                {
                    case AssetType.Manifest: editor = new Manifest.ManifestEditor(App, fullAssetPath); break;
                    case AssetType.TileSet: editor = new TileSet.TileSetEditor(App, fullAssetPath); break;
                    case AssetType.Script: editor = new Script.ScriptEditor(App, fullAssetPath); break;
                    case AssetType.Image: editor = new Image.ImageEditor(App, fullAssetPath); break;
                    case AssetType.Map: editor = new Map.MapEditor(App, fullAssetPath); break;
                    default: throw new NotSupportedException();
                }

                assetUI = new OpenAssetUI { Tab = tab, Editor = editor };
                _openAssetUIsByEntry.Add(entry, assetUI);
            }

            // TODO:
            // _activeAssetUI?.Tab.SetActive(false);
            // _activeAssetUI = assetUI;
            // _activeAssetUI.Tab.SetActive(true);

            _activeEditorContainer.Clear();
            _activeEditorContainer.Add(assetUI.Editor);
            _activeEditorContainer.Layout();
        }

        void CloseAsset(AssetEntry entry)
        {
            if (!_openAssetUIsByEntry.TryGetValue(entry, out var assetUI)) return;

            assetUI.Editor.MaybeUnload(() =>
            {
                _openAssetUIsByEntry.Remove(entry);

                _tabsBar.Remove(assetUI.Tab);
                _tabsBar.Layout();

                if (assetUI.Editor.IsMounted)
                {
                    // TODO: Make another asset active
                    _activeEditorContainer.Clear();
                }
            });
        }

        public void OnAssetCreated(AssetEntry entry)
        {
            _assetTree.AddEntry(entry);
            _assetTree.ShowEntry(entry);
            _assetTree.Layout();
        }

        public void OnAssetDeleted(AssetEntry entry)
        {
            _assetTree.DeleteEntry(entry);
            _assetTree.Layout();
        }
    }
}
