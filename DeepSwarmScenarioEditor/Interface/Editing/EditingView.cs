﻿using DeepSwarmCommon;
using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.Interface;
using DeepSwarmPlatform.UI;
using DeepSwarmScenarioEditor.Scenario;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DeepSwarmScenarioEditor.Interface.Editing
{
    class EditingView : InterfaceElement
    {
        readonly Panel _sidebarPanel;
        readonly AssetTree _assetTree;

        readonly Panel _mainPanel;
        readonly Element _editorContainer;

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
                LayoutWeight = 1
            };

            new StyledTextButton(topBar)
            {
                Text = "Run",
                OnActivate = () =>
                {
                    var clientExePath = Path.Combine(FileHelper.FindAppFolder("DeepSwarmClient-Debug"), "netcoreapp3.0", "DeepSwarmClient.exe");
                    Process.Start(clientExePath, "--scenario " + Engine.State.ActiveScenarioEntry.Name);
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

            MakeAssetEntries(null, Engine.State.AssetEntries);

            Desktop.SetFocusedElement(this);
        }

        public void OnActiveAssetChanged()
        {
            _editorContainer.Clear();

            var entry = Engine.State.ActiveAssetEntry;
            var fullAssetPath = Path.Combine(Engine.State.ScenariosPath, Engine.State.ActiveScenarioEntry.Name, entry.Path);
            Element editor = null;

            switch (entry.AssetType)
            {
                case AssetType.Unknown:
                case AssetType.Folder:
                    break;

                case AssetType.Manifest: editor = new Manifest.ManifestEditor(Engine.Interface, fullAssetPath); break;
                case AssetType.TileSet: editor = new TileSet.TileSetEditor(Engine.Interface, fullAssetPath); break;
                case AssetType.Script: editor = new Script.ScriptEditor(Engine.Interface, fullAssetPath); break;
                case AssetType.Image: editor = new Image.ImageEditor(Engine.Interface, fullAssetPath); break;
                case AssetType.Map: editor = new Map.MapEditor(Engine.Interface, fullAssetPath); break;
            }

            if (editor != null)
            {
                _editorContainer.Add(editor);
                Desktop.SetFocusedElement(editor);
            }

            _editorContainer.Layout();
        }
    }
}
