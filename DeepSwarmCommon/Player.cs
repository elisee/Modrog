using System;

namespace DeepSwarmCommon
{
    public class Player
    {
        public enum PlayerTeam { Blue, Red }

        public Guid Guid;
        public string Name;
        public PlayerTeam Team;
        public int BaseChunkX;
        public int BaseChunkY;
    }
}
