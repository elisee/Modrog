using DeepSwarmBasics.Math;
using System.Collections.Generic;

namespace DeepSwarmServer.Game
{
    class InternalPlayer : DeepSwarmApi.Server.Player
    {
        internal int Index;
        internal string Name;

        internal InternalWorld World;

        // Will be cleared after having been sent to the player
        public Point? TeleportPosition;

        internal readonly List<InternalEntity> OwnedEntities = new List<InternalEntity>();
        internal readonly Dictionary<int, InternalEntity> OwnedEntitiesById = new Dictionary<int, InternalEntity>();
        internal readonly HashSet<InternalEntity> TrackedEntities = new HashSet<InternalEntity>();

        #region API
        public override void Teleport(DeepSwarmApi.Server.World world, Point position)
        {
            World = (InternalWorld)world;
            TeleportPosition = position;
        }

        public override void ShowTip(string tip)
        {
            // TODO
        }
        #endregion
    }
}
