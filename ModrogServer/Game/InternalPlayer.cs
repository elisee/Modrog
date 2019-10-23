using SwarmBasics.Math;
using System.Collections.Generic;

namespace ModrogServer.Game
{
    sealed class InternalPlayer : ModrogApi.Server.Player
    {
        internal int Index;
        internal string Name;

        internal InternalWorld World;
        public Point Position;
        public bool WasJustTeleported;

        internal readonly List<InternalEntity> OwnedEntities = new List<InternalEntity>();
        internal readonly Dictionary<int, InternalEntity> OwnedEntitiesById = new Dictionary<int, InternalEntity>();
        internal readonly HashSet<InternalEntity> TrackedEntities = new HashSet<InternalEntity>();

        #region API
        public override void Teleport(ModrogApi.Server.World world, Point position)
        {
            World = (InternalWorld)world;
            Position = position;
            WasJustTeleported = true;
        }

        public override void ShowTip(string tip)
        {
            // TODO
        }
        #endregion
    }
}
