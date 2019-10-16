namespace DeepSwarmScenarioEditor
{
    enum EditorStage { Home, Editing, Exited }

    partial class EditorState
    {
        public EditorStage Stage;

        readonly Engine _engine;

        public EditorState(Engine engine) { _engine = engine; }

        public void Stop()
        {
            Stage = EditorStage.Exited;
        }

        internal void Update(float deltaTime)
        {
        }
    }
}
