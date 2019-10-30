using ModrogEditor.Scenario;
using SDL2;
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

        readonly NewAssetLayer _newAssetLayer;
        readonly DeleteAssetLayer _deleteAssetLayer;
        readonly ConfirmCloseLayer _confirmCloseLayer;
        readonly ErrorLayer _errorLayer;

        readonly Panel _mainPanel;
        readonly Element _tabsBar;
        public Element _activeEditorContainer;

        public class EditorUI { public EditorTabButton Tab; public BaseEditor Editor; }
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

            new StyledTextButton(headerBar)
            {
                Text = "New",
                OnActivate = () =>
                {
                    _newAssetLayer.Visible = true;
                    _newAssetLayer.Layout(_contentRectangle);
                }
            };

            _newAssetLayer = new NewAssetLayer(this);
            _deleteAssetLayer = new DeleteAssetLayer(this);

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
                BackgroundPatch = new TexturePatch(0x001234ff),
            };

            _tabsBar = new Element(topBar)
            {
                LayoutWeight = 1,
                ChildLayout = ChildLayoutMode.Left,
                HorizontalFlow = Flow.Scroll,
                ScrollbarThickness = 4,
            };

            new StyledTextButton(topBar) { Text = "Run", OnActivate = RunScenario };

            _activeEditorContainer = new Element(_mainPanel) { LayoutWeight = 1 };

            _confirmCloseLayer = new ConfirmCloseLayer(this);
            _errorLayer = new ErrorLayer(this);
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

        public override void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            if (key == SDL.SDL_Keycode.SDLK_s && Desktop.IsCtrlOnlyDown)
            {
                if (_activeEditorUI != null)
                {
                    SaveEditor(_activeEditorUI.Editor);
                    return;
                }
            }

            if (key == SDL.SDL_Keycode.SDLK_w && Desktop.IsCtrlOnlyDown)
            {
                if (_activeEditorUI != null)
                {
                    ConfirmCloseEditor(_activeEditorUI, onEditorClosed: null);
                    return;
                }
            }

            if (key == SDL.SDL_Keycode.SDLK_F5 && !repeat && Desktop.HasNoKeyModifier)
            {
                RunScenario();
                return;
            }

            if (key == SDL.SDL_Keycode.SDLK_TAB && (Desktop.IsCtrlOnlyDown || Desktop.IsCtrlShiftDown))
            {
                if (_tabsBar.Children.Count > 0)
                {
                    _activeEditorUI.Tab.SetActive(false);
                    _activeEditorContainer.Clear();

                    var tabIndex = _tabsBar.Children.IndexOf(_activeEditorUI.Tab);
                    var newTabIndex = (tabIndex + _tabsBar.Children.Count + (Desktop.IsShiftDown ? -1 : 1)) % _tabsBar.Children.Count;
                    var siblingTab = (EditorTabButton)_tabsBar.Children[newTabIndex];

                    siblingTab.SetActive(true);
                    _activeEditorUI = _openEditorUIsByEntry[siblingTab.Entry];
                    _activeEditorContainer.Add(_activeEditorUI.Editor);
                    _activeEditorContainer.Layout();
                }
            }

            base.OnKeyDown(key, repeat);
        }

        void RunScenario()
        {
            var clientExePath = Path.Combine(FileHelper.FindAppFolder(
#if DEBUG
                "ModrogClient-Debug"
#else
                "ModrogClient-Release"
#endif
                ), "netcoreapp3.0", "ModrogClient.exe");
            Process.Start(clientExePath, "--scenario " + App.State.ActiveScenarioEntry.Name);
        }

        internal AssetEntry GetSelectedAssetTreeFolderEntry()
        {
            var entry = _assetTree.GetSelectedEntry() ?? App.State.RootAssetEntry;
            return entry.AssetType == AssetType.Folder ? entry : entry.Parent;
        }

        internal void OpenOrFocusEditor(AssetEntry entry)
        {
            if (!_openEditorUIsByEntry.TryGetValue(entry, out var editorUI))
            {
                void onCloseEditor() => ConfirmCloseEditor(editorUI, onEditorClosed: null);

                var tab = new EditorTabButton(_tabsBar, entry)
                {
                    OnActivate = () => OpenOrFocusEditor(entry),
                    OnClose = onCloseEditor
                };

                _tabsBar.Layout();

                var fullAssetPath = Path.Combine(App.State.ActiveScenarioPath, entry.Path);
                void onUnsavedStatusChanged() => tab.SetUnsavedChanges(editorUI.Editor.HasUnsavedChanges);

                BaseEditor editor = entry.AssetType switch
                {
                    AssetType.Manifest => new Manifest.ManifestEditor(App, fullAssetPath, onUnsavedStatusChanged),
                    AssetType.TileSet => new TileSet.TileSetEditor(App, fullAssetPath, onUnsavedStatusChanged),
                    AssetType.Script => new Script.ScriptEditor(App, fullAssetPath, onUnsavedStatusChanged),
                    AssetType.Image => new Image.ImageEditor(App, fullAssetPath, onUnsavedStatusChanged),
                    AssetType.Map => new Map.MapEditor(App, fullAssetPath, onUnsavedStatusChanged),
                    _ => throw new NotSupportedException(),
                };

                editorUI = new EditorUI { Tab = tab, Editor = editor };
                _openEditorUIsByEntry.Add(entry, editorUI);
            }

            _activeEditorUI?.Tab.SetActive(false);
            _activeEditorUI = editorUI;
            _activeEditorUI.Tab.SetActive(true);

            _activeEditorContainer.Clear();
            _activeEditorContainer.Add(editorUI.Editor);
            _activeEditorContainer.Layout();
        }

        public void ConfirmCloseAllEditors(Action onAllEditorsClosed)
        {
            void TryAgainAfterClosingEditor() => ConfirmCloseAllEditors(onAllEditorsClosed);

            if (_activeEditorUI != null)
            {
                ConfirmCloseEditor(_activeEditorUI, TryAgainAfterClosingEditor);
                return;
            }

            onAllEditorsClosed?.Invoke();
        }

        void SaveEditor(BaseEditor editor)
        {
            if (!editor.TrySave(out var error))
            {
                _errorLayer.Open("Failed to save", "Error while saving asset: " + error, onTryAgain: () => SaveEditor(editor));
                _errorLayer.Layout(_contentRectangle);
                Desktop.SetFocusedElement(_errorLayer);
            }
        }

        void ConfirmCloseEditor(EditorUI editorUI, Action onEditorClosed)
        {
            if (!editorUI.Editor.HasUnsavedChanges)
            {
                RemoveEditor(editorUI);
                onEditorClosed?.Invoke();
                return;
            }

            OpenOrFocusEditor(editorUI.Tab.Entry);

            _confirmCloseLayer.Open(editorUI, onClose: () =>
            {
                RemoveEditor(editorUI);
                onEditorClosed?.Invoke();
            });

            _confirmCloseLayer.Layout(_contentRectangle);
            Desktop.SetFocusedElement(_confirmCloseLayer);
        }

        void RemoveEditor(EditorUI editorUI)
        {
            editorUI.Editor.Unload();

            _openEditorUIsByEntry.Remove(editorUI.Tab.Entry);

            var tabIndex = _tabsBar.Children.IndexOf(editorUI.Tab);

            _tabsBar.Remove(editorUI.Tab);
            _tabsBar.Layout();

            if (editorUI.Editor.IsMounted)
            {
                _activeEditorContainer.Clear();
                _activeEditorUI = null;

                if (_tabsBar.Children.Count > 0)
                {
                    var siblingTab = (EditorTabButton)_tabsBar.Children[Math.Min(tabIndex, _tabsBar.Children.Count - 1)];
                    siblingTab.SetActive(true);

                    _activeEditorUI = _openEditorUIsByEntry[siblingTab.Entry];
                    _activeEditorContainer.Add(_activeEditorUI.Editor);
                    _activeEditorContainer.Layout();
                }
            }
        }

        public void OnAssetCreated(AssetEntry entry)
        {
            _assetTree.AddEntry(entry);
            _assetTree.ShowEntry(entry);
            _assetTree.SetSelectedEntry(entry);

            _assetTree.Layout();
        }

        public void OnAssetDeleted(AssetEntry entry)
        {
            _assetTree.DeleteEntry(entry);
            _assetTree.Layout();

            TryCloseAssetEditor(entry);

            void TryCloseAssetEditor(AssetEntry entry)
            {
                if (entry.AssetType == AssetType.Folder)
                {
                    foreach (var childEntry in entry.Children) TryCloseAssetEditor(childEntry);
                }
                else
                {
                    if (_openEditorUIsByEntry.TryGetValue(entry, out var editorUI)) RemoveEditor(editorUI);
                }
            }
        }
    }
}
