﻿namespace DeepSwarmClient
{
    class PlayerListEntry
    {
        public string Name;
        public bool IsHost;
        public bool IsOnline;

        public static string GetEntryLabel(PlayerListEntry entry) => $"[{(entry.IsOnline ? "ON" : "OFF")}] {(entry.IsHost ? "* " : "")}{entry.Name}";
    }
}
