namespace DeepSwarmCommon
{
    public static class Protocol
    {
        public const int Port = 8798;
        public const int MaxPlayerNameLength = 16;
        public static readonly string VersionString = "DEEPSWARM0";

        public enum ServerPacketType : byte
        {
            PlayerList,
            Setup,
            Tick,
        }
    }
}
