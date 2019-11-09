using ModrogApi;
using ModrogApi.Server;
using SwarmBasics.Math;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ModrogServer.Game
{
    sealed class InternalEntity : Entity, IEquatable<InternalEntity>
    {
        internal readonly int Id;
        internal InternalWorld World;
        public Point SpriteLocation;
        internal Point Position;
        internal Point PreviousTickPosition;
        internal int PlayerIndex;

        internal EntityIntent UpcomingIntent = EntityIntent.Idle;
        internal EntityAction EffectiveAction = EntityAction.Idle;

        public int ViewRadius = 2;


        public InternalEntity(int id, InternalWorld world, Point spriteLocation, Point position, int playerIndex)
        {
            Id = id;
            SpriteLocation = spriteLocation;
            Position = PreviousTickPosition = position;

            PlayerIndex = playerIndex;
            if (PlayerIndex != -1)
            {
                world.Universe.Players[PlayerIndex].OwnedEntities.Add(this);
                world.Universe.Players[PlayerIndex].OwnedEntitiesById.Add(Id, this);
            }

            world.Add(this);
        }

        public override bool Equals(object obj) => Equals(obj as InternalEntity);
        public bool Equals([AllowNull] InternalEntity other) => other != null && Id == other?.Id;
        public override int GetHashCode() => HashCode.Combine(Id);

        public override void SetView(int omniViewRadius)
        {
            ViewRadius = omniViewRadius;
        }

        #region API
        public override World GetWorld() => World;
        public override Point GetPosition() => Position;

        public override void Teleport(World world, Point position)
        {
            World.Remove(this);
            ((InternalWorld)world).Add(this);

            Position = PreviousTickPosition = position;
            EffectiveAction = EntityAction.Idle;
        }
        #endregion
    }
}
