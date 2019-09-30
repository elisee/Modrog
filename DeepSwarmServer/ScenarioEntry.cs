namespace DeepSwarmServer
{
    class ScenarioEntry
    {
        public string Name;
        public string Description;
        public int MinPlayers;
        public int MaxPlayers;

        public enum ScenarioMode { Cooperative, Competitive, Both }
        public ScenarioMode SupportedModes;
    }
}
