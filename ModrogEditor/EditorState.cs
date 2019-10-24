﻿using ModrogCommon;
using ModrogEditor.Interface.Editing.Image;
using ModrogEditor.Interface.Editing.Map;
using ModrogEditor.Interface.Editing.Script;
using ModrogEditor.Interface.Editing.TileSet;
using ModrogEditor.Scenario;
using SwarmCore;
using System;
using System.Collections.Generic;
using System.IO;

namespace ModrogEditor
{
    enum EditorStage { Home, Editing, Exited }

    partial class EditorState
    {
        readonly EditorApp _app;

        public EditorStage Stage;

        public readonly string ScenariosPath;
        public readonly List<ScenarioEntry> ScenarioEntries = new List<ScenarioEntry>();
        public readonly AssetEntry RootAssetEntry = new AssetEntry()
        {
            Name = "Root",
            Path = "",
            AssetType = AssetType.Folder
        };

        public ScenarioEntry ActiveScenarioEntry { get; private set; }
        public AssetEntry ActiveAssetEntry { get; private set; }
        public string ActiveScenarioPath => Path.Combine(ScenariosPath, ActiveScenarioEntry.Name);

        public EditorState(EditorApp app)
        {
            _app = app;

            ScenariosPath = FileHelper.FindAppFolder("Scenarios");
            ScenarioEntries = ScenarioEntry.ReadScenarioEntries(ScenariosPath);
        }

        public void Stop()
        {
            Stage = EditorStage.Exited;
        }

        internal void Update(float deltaTime)
        {
        }

        public void OpenScenario(ScenarioEntry entry)
        {
            ActiveScenarioEntry = entry;
            var scenarioPath = Path.Combine(ScenariosPath, entry.Name);

            void Recurse(AssetEntry parentEntry, string folderPath)
            {
                foreach (var filePath in Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var entry = CreateEntry(filePath[(folderPath.Length + 1)..], filePath[(scenarioPath.Length + 1)..].Replace('\\', '/'), parentEntry);
                    parentEntry.Children.Add(entry);
                }

                foreach (var childFolderPath in Directory.GetDirectories(folderPath))
                {
                    var folderEntry = CreateEntry(childFolderPath[(folderPath.Length + 1)..], childFolderPath[(scenarioPath.Length + 1)..].Replace('\\', '/'), parentEntry);
                    parentEntry.Children.Add(folderEntry);

                    Recurse(folderEntry, childFolderPath);
                }
            }

            RootAssetEntry.Children.Clear();
            Recurse(RootAssetEntry, scenarioPath);

            ActiveAssetEntry = RootAssetEntry;

            Stage = EditorStage.Editing;
            _app.OnStageChanged();
        }

        public bool TryCreateAsset(string assetName, out AssetEntry assetEntry, out string error)
        {
            var parentEntry = ActiveAssetEntry;
            if (parentEntry.AssetType != AssetType.Folder) parentEntry = parentEntry.Parent;

            var assetPath = Path.Combine(parentEntry.Path, assetName);
            var fullAssetPath = Path.Combine(ActiveScenarioPath, assetPath);

            assetEntry = CreateEntry(assetName, assetPath, parentEntry);

            if (assetEntry.AssetType == AssetType.Unknown)
            {
                assetEntry = null;
                error = "File's asset type is unknown.";
                return false;
            }
            else if (assetEntry.AssetType == AssetType.Folder && Directory.Exists(fullAssetPath))
            {
                assetEntry = null;
                error = "Folder already exists.";
                return false;
            }
            else if (File.Exists(fullAssetPath))
            {
                assetEntry = null;
                error = "File already exists.";
                return false;
            }

            switch (assetEntry.AssetType)
            {
                case AssetType.Folder: Directory.CreateDirectory(fullAssetPath); break;
                case AssetType.Image: ImageEditor.CreateEmptyFile(fullAssetPath); break;
                case AssetType.Map: MapEditor.CreateEmptyFile(fullAssetPath); break;
                case AssetType.TileSet: TileSetEditor.CreateEmptyFile(fullAssetPath); break;
                case AssetType.Script: ScriptEditor.CreateEmptyFile(fullAssetPath); break;
                default: throw new NotImplementedException();
            }

            parentEntry.Children.Add(assetEntry);
            _app.EditingView.OnAssetCreated(assetEntry);

            error = null;
            return true;
        }

        AssetEntry CreateEntry(string assetName, string assetPath, AssetEntry parentEntry)
        {
            var entry = new AssetEntry
            {
                Name = assetName,
                Path = assetPath,
                Parent = parentEntry
            };

            if (entry.Path == "Manifest.json") entry.AssetType = AssetType.Manifest;
            else if (entry.Name.EndsWith(".png")) entry.AssetType = AssetType.Image;
            else if (entry.Name.EndsWith(".tileset")) entry.AssetType = AssetType.TileSet;
            else if (entry.Name.EndsWith(".map")) entry.AssetType = AssetType.Map;
            else if (entry.Name.EndsWith(".cs")) entry.AssetType = AssetType.Script;
            else if (entry.Name.IndexOf(".") != -1) entry.AssetType = AssetType.Unknown;
            else entry.AssetType = AssetType.Folder;

            return entry;
        }

        public void OpenAsset(AssetEntry entry)
        {
            ActiveAssetEntry = entry;
            _app.EditingView.OnActiveAssetChanged();
        }
    }
}
