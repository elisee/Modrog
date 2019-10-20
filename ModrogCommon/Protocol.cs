﻿using System.Text.RegularExpressions;

namespace ModrogCommon
{
    public static class Protocol
    {
        public const int Port = 8798;
        public const int MaxPlayerNameLength = 16;
        public static readonly Regex PlayerNameRegex = new Regex(@"^[A-Za-z0-9_]{1,16}$");
        public static readonly string VersionString = "MODROG0";
        public const int MaxScriptNameLength = 64;

        public const float StartCountdownDuration = 3f;
        public const float TickInterval = 0.2f;

        public const int MapTileSize = 20;
        public const int MapChunkSide = 16;
        public enum MapLayer
        {
            Floor,
            Fluid,
            Wall,

            Count
        }

        public enum ServerPacketType : byte
        {
            // Handshake
            Welcome,
            Kick,

            // Lobby and Playing
            Chat,
            PeerList,

            // Lobby,
            SetScenario,
            SetupCountdown,

            // Playing
            SetPeerOnline,
            UniverseSetup,
            Tick,
        }

        public enum ClientPacketType : byte
        {
            // Handshake
            Hello,

            // Lobby and Playing
            Chat,
            StopGame,

            // Lobby
            SetScenario,
            Ready,
            StartGame,

            // Playing
            SetPosition,
            PlanMoves,
        }
    }
}