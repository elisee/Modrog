using static DeepSwarmCommon.Player;

namespace DeepSwarmClient
{
    class PlayerListEntry
    {
        public string Name;
        public PlayerTeam Team;
        public bool IsOnline;

        public static string GetEntryLabel(PlayerListEntry entry) => $"[{entry.Team.ToString()}] {entry.Name}{(entry.IsOnline ? "" : " (offline)")}";
    }
}
