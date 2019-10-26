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

        class EditorUI { public EditorTabButton Tab; public BaseEditor Editor; }
        readonly Dictionary<AssetEntry, EditorUI> _openEditorUIsByEntry = new Dictionary<AssetEntry, EditorUI>();
        EditorUI _activeEditorUI;

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
                    if (entry.AssetType != AssetType.Folder && entry.AssetType != AssetType.Unknown) OpenOrFocusEditor(entry);
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
                ChildLayout = ChildLayoutMode.Left,
                HorizontalFlow = Flow.Scroll
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

        internal void OpenOrFocusEditor(AssetEntry entry)
        {
            if (!_openEditorUIsByEntry.TryGetValue(entry, out var assetEditorUI))
            {
                var tab = new EditorTabButton(_tabsBar, entry)
                {
                    OnActivate = () => OpenOrFocusEditor(entry),
                    OnClose = () => CloseEditor(entry)
                };

                _tabsBar.Layout();

                var fullAssetPath = Path.Combine(App.State.ActiveScenarioPath, entry.Path);
                BaseEditor editor;

                switch (entry.AssetType)
                {
                    case AssetType.Manifest: editor = new Manifest.ManifestEditor(App, fullAssetPath); break;
                    case AssetType.TileSet: editor = new TileSet.TileSetEditor(App, fullAssetPath); break;
                    case AssetType.Script: editor = new Script.ScriptEditor(App, fullAssetPath); break;
                    case AssetType.Image: editor = new Image.ImageEditor(App, fullAssetPath); break;
                    case AssetType.Map: editor = new Map.MapEditor(App, fullAssetPath); break;
                    default: throw new NotSupportedException();
                }

                assetEditorUI = new EditorUI { Tab = tab, Editor = editor };
                _openEditorUIsByEntry.Add(entry, assetEditorUI);
            }

            _activeEditorUI?.Tab.SetActive(false);
            _activeEditorUI = assetEditorUI;
            _activeEditorUI.Tab.SetActive(true);

            _activeEditorContainer.Clear();
            _activeEditorContainer.Add(assetEditorUI.Editor);
            _activeEditorContainer.Layout();
        }

        void CloseEditor(AssetEntry entry)
        {
            if (!_openEditorUIsByEntry.TryGetValue(entry, out var assetUI)) return;

            assetUI.Editor.MaybeUnload(() =>
            {
                _openEditorUIsByEntry.Remove(entry);

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
