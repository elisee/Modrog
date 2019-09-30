using System;
using System.Collections.Generic;

namespace DeepSwarmServer
{
    class SavedGameEntry
    {
        public DateTime Date;
        public string Scenario;
        public readonly List<string> PlayerNames = new List<string>();
    }
}
