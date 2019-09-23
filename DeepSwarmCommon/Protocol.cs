namespace DeepSwarmCommon
{
    public static class Protocol
    {
        public const int Port = 8798;
        public const int MaxPlayerNameLength = 16;
        public static readonly string VersionString = "DEEPSWARM0";

        public const int MaxScriptNameLength = 64;

        public enum ServerPacketType : byte
        {
            SetupPlayerIndex,
            PlayerList,
            Tick,
            Chat,
        }

        public enum ClientPacketType : byte
        {
            PlanMoves,
            Chat,
        }
    }
}
