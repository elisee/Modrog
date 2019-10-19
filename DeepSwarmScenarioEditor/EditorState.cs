using DeepSwarmCommon;
using DeepSwarmScenarioEditor.Scenario;
using System.Collections.Generic;
using System.IO;

namespace DeepSwarmScenarioEditor
{
    enum EditorStage { Home, Editing, Exited }

    partial class EditorState
    {
        public EditorStage Stage;

        readonly Engine _engine;

        public readonly string ScenariosPath;
        public readonly List<ScenarioEntry> ScenarioEntries = new List<ScenarioEntry>();
        public readonly List<AssetEntry> AssetEntries = new List<AssetEntry>();

        public ScenarioEntry ActiveScenarioEntry { get; private set; }
        public AssetEntry ActiveAssetEntry { get; private set; }
        public string ActiveScenarioPath => Path.Combine(ScenariosPath, ActiveScenarioEntry.Name);

        public EditorState(Engine engine)
        {
            _engine = engine;

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

            void Recurse(List<AssetEntry> siblings, string folderPath)
            {
                foreach (var filePath in Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var entry = new AssetEntry
                    {
                        Name = filePath[(folderPath.Length + 1)..],
                        Path = filePath[(scenarioPath.Length + 1)..].Replace('\\', '/'),
                    };

                    if (entry.Path == "Manifest.json") entry.AssetType = AssetType.Manifest;
                    else if (entry.Name.EndsWith(".png")) entry.AssetType = AssetType.Image;
                    else if (entry.Name.EndsWith(".tileset")) entry.AssetType = AssetType.TileSet;
                    else if (entry.Name.EndsWith(".map")) entry.AssetType = AssetType.Map;
                    else if (entry.Name.EndsWith(".cs")) entry.AssetType = AssetType.Script;

                    siblings.Add(entry);
                }

                foreach (var childFolderPath in Directory.GetDirectories(folderPath))
                {
                    var folderEntry = new AssetEntry
                    {
                        Name = childFolderPath[(folderPath.Length + 1)..],
                        Path = childFolderPath[(scenarioPath.Length + 1)..].Replace('\\', '/'),
                        AssetType = AssetType.Folder
                    };

                    siblings.Add(folderEntry);
                    Recurse(folderEntry.Children, childFolderPath);
                }
            }

            Recurse(AssetEntries, scenarioPath);

            Stage = EditorStage.Editing;
            _engine.Interface.OnStageChanged();
        }

        public void OpenAsset(AssetEntry entry)
        {
            ActiveAssetEntry = entry;
            _engine.Interface.EditingView.OnActiveAssetChanged();
        }
    }
}
