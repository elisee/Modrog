using System;
using System.Collections.Generic;

namespace DeepSwarmCommon
{
    public class SavedGameEntry
    {
        public string ScenarioName;
        public int ScenarioVersion;
        public readonly List<string> PlayerNames = new List<string>();
        public DateTime Date;
    }
}
