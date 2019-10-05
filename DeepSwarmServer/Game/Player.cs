using System.Collections.Generic;

namespace DeepSwarmServer.Game
{
    class Player
    {
        public int Id;
        public string Name;

        public World World;
        public readonly List<Entity> OwnedEntities = new List<Entity>();
        public readonly HashSet<Entity> TrackedEntities = new HashSet<Entity>();
    }
}
