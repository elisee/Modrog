using DeepSwarmApi;
using DeepSwarmApi.Server;
using DeepSwarmBasics.Math;
using System;

namespace DeepSwarmServer.Game
{
    public class InternalEntity : Entity
    {
        internal readonly int Id;
        internal InternalWorld World;
        public Point SpriteLocation;
        internal Point Position;
        internal EntityDirection Direction;
        internal int PlayerIndex;

        internal EntityMove UpcomingMove = EntityMove.Idle;

        public int OmniViewRadius = 2;
        public int DirectionalViewRadius = 8;
        public float HalfFieldOfView = MathF.PI / 3f;

        public InternalEntity(int id, InternalWorld world, Point spriteLocation, Point position, EntityDirection direction, int playerIndex)
        {
            Id = id;
            SpriteLocation = spriteLocation;
            Position = position;
            Direction = direction;

            PlayerIndex = playerIndex;
            if (PlayerIndex != -1)
            {
                world.Universe.Players[PlayerIndex].OwnedEntities.Add(this);
                world.Universe.Players[PlayerIndex].OwnedEntitiesById.Add(Id, this);
            }

            world.Add(this);
        }

        internal float GetDirectionAngle() => Direction switch
        {
            EntityDirection.Right => 0f,
            EntityDirection.Down => MathF.PI / 2f,
            EntityDirection.Left => MathF.PI,
            EntityDirection.Up => MathF.PI * 3f / 2f,
            _ => throw new NotSupportedException(),
        };

        #region API
        public override World GetWorld() => World;
        public override Point GetPosition() => Position;

        public override void Teleport(World world, Point position, EntityDirection direction)
        {
            ((InternalWorld)World).Remove(this);
            ((InternalWorld)world).Add(this);

            Position = position;
            Direction = direction;
        }
        #endregion
    }
}
