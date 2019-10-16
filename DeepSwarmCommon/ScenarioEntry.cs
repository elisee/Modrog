using System.Collections.Generic;
using System.IO;
using System.Json;

namespace DeepSwarmCommon
{
    public class ScenarioEntry
    {
        public string Name;

        public string Title;
        public int MinPlayers;
        public int MaxPlayers;
        public bool SupportsCoop;
        public bool SupportsVersus;
        public string Description;

        public static List<ScenarioEntry> ReadScenarioEntries(string path)
        {
            var entries = new List<ScenarioEntry>();

            foreach (var folder in Directory.GetDirectories(path, "*.*", SearchOption.TopDirectoryOnly))
            {
                var manifestJson = JsonValue.Parse(File.ReadAllText(Path.Combine(folder, "Manifest.json")));

                var entry = new ScenarioEntry
                {
                    Name = folder[(path.Length + 1)..],
                    Title = manifestJson["title"],
                    MinPlayers = manifestJson["minMaxPlayers"][0],
                    MaxPlayers = manifestJson["minMaxPlayers"][1],
                    SupportsCoop = manifestJson.ContainsKey("supportsCoop") && manifestJson["supportsCoop"],
                    SupportsVersus = manifestJson.ContainsKey("supportsVersus") && manifestJson["supportsVersus"],
                    Description = manifestJson["description"]
                };

                entries.Add(entry);
            }

            return entries;
        }
    }
}
