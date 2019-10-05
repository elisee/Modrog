using System.Collections.Generic;

namespace DeepSwarmServer.Game
{
    class Player
    {
        public PlayerIdentity Identity;
        public int PlayerIndex;

        public World World;
        public readonly List<Entity> OwnedEntities = new List<Entity>();
        public readonly HashSet<Entity> TrackedEntities = new HashSet<Entity>();
    }
}
