using DeepSwarmCommon;
using System.Collections.Generic;

namespace DeepSwarmScenarioEditor
{
    enum EditorStage { Home, Editing, Exited }

    partial class EditorState
    {
        public EditorStage Stage;

        readonly Engine _engine;

        readonly string _scenariosPath;
        public readonly List<ScenarioEntry> ScenarioEntries = new List<ScenarioEntry>();
        public ScenarioEntry ActiveScenarioEntry { get; private set; }

        public EditorState(Engine engine)
        {
            _engine = engine;

            _scenariosPath = FileHelper.FindAppFolder("Scenarios");
            ScenarioEntries = ScenarioEntry.ReadScenarioEntries(_scenariosPath);
        }

        public void Stop()
        {
            Stage = EditorStage.Exited;
        }

        internal void Update(float deltaTime)
        {
        }

        internal void OpenScenario(ScenarioEntry entry)
        {
            ActiveScenarioEntry = entry;
            Stage = EditorStage.Editing;
            _engine.Interface.OnStageChanged();
        }
    }
}
