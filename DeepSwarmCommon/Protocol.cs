using System.Text.RegularExpressions;

namespace DeepSwarmCommon
{
    public static class Protocol
    {
        public const int Port = 8798;
        public const int MaxPlayerNameLength = 16;
        public static readonly Regex PlayerNameRegex = new Regex(@"^[A-Za-z0-9_]{1,16}$");
        public static readonly string VersionString = "DEEPSWARM0";
        public const int MaxScriptNameLength = 64;

        public const float StartCountdownDuration = 3f;
        public const float TickInterval = 0.2f;

        public const int MapTileSize = 20;
        public const int MapChunkSide = 16;
        public enum MapLayer
        {
            // Needs to allow stacking a floor or water with a path or some wood planks
            // Need to allow stacking a wall on a floor, the floor getting revealed when the wall is destroyed
            // Need to allow showing tiles above the player
            // Buuuut, that might be separate from what logical tile is at a location
            // A TileKind can be configured to display various things at various levels right?

            // So, if we don't worry about the look, we need to know, in ALL cases:
            // - what is the floor we are sitting on, if any (might be emptiness)
            // - whether there is some fluid that digs into the floor
            // - whether there is some wall that sits on the floor
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
