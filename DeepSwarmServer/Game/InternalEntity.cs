using DeepSwarmBasics.Math;
using System;

namespace DeepSwarmServer.Game
{
    public class InternalEntity : DeepSwarmApi.Server.Entity
    {
        internal readonly int Id;
        internal InternalWorld World;
        internal Point Position;
        internal DeepSwarmApi.EntityDirection Direction;
        internal int PlayerIndex;

        internal DeepSwarmApi.EntityMove UpcomingMove = DeepSwarmApi.EntityMove.Idle;

        public int OmniViewRadius = 2;
        public int DirectionalViewRadius = 8;
        public float HalfFieldOfView = MathF.PI / 3f;

        public InternalEntity(int id, InternalWorld world, Point position, DeepSwarmApi.EntityDirection direction, int playerIndex)
        {
            Id = id;
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
            DeepSwarmApi.EntityDirection.Right => 0f,
            DeepSwarmApi.EntityDirection.Down => MathF.PI / 2f,
            DeepSwarmApi.EntityDirection.Left => MathF.PI,
            DeepSwarmApi.EntityDirection.Up => MathF.PI * 3f / 2f,
            _ => throw new NotSupportedException(),
        };

        #region API
        #endregion
    }
}
