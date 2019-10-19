using SwarmCore;
using System.Collections.Generic;
using System.IO;

namespace ModrogCommon
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
                var manifestJson = JsonHelper.Parse(File.ReadAllText(Path.Combine(folder, "Manifest.json")));

                var entry = new ScenarioEntry
                {
                    Name = folder[(path.Length + 1)..],
                    Title = manifestJson.GetProperty("title").GetString(),
                    MinPlayers = manifestJson.GetProperty("minMaxPlayers")[0].GetInt32(),
                    MaxPlayers = manifestJson.GetProperty("minMaxPlayers")[1].GetInt32(),
                    SupportsCoop = manifestJson.TryGetProperty("supportsCoop", out var supportsCoop) && supportsCoop.GetBoolean(),
                    SupportsVersus = manifestJson.TryGetProperty("supportsVersus", out var supportsVersus) && supportsVersus.GetBoolean(),
                    Description = manifestJson.GetProperty("description").GetString()
                };

                entries.Add(entry);
            }

            return entries;
        }
    }
}
