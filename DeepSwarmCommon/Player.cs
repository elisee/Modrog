using System.Collections.Generic;

namespace DeepSwarmCommon
{
    public class Player
    {
        public PlayerIdentity Identity;
        public int PlayerIndex;

        public readonly List<Entity> OwnedEntities = new List<Entity>();
        public readonly HashSet<Entity> TrackedEntities = new HashSet<Entity>();
    }
}
