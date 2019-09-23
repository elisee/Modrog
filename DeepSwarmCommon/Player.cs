using System;
using System.Collections.Generic;

namespace DeepSwarmCommon
{
    public class Player
    {
        public enum PlayerTeam : byte { None, Blue, Red }

        public int PlayerIndex;
        public Guid Guid;

        public string Name;
        public PlayerTeam Team;
        public int BaseChunkX;
        public int BaseChunkY;

        public readonly List<Entity> OwnedEntities = new List<Entity>();
        public readonly HashSet<Entity> TrackedEntities = new HashSet<Entity>();
    }
}
