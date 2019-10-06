using DeepSwarmBasics.Math;
using System;

namespace DeepSwarmServer.Game
{
    public class InternalEntity : DeepSwarmApi.Server.Entity
    {
        internal readonly int Id;
        internal InternalWorld World;
        internal Point Position;
        internal DeepSwarmApi.Server.EntityDirection Direction;
        internal int PlayerIndex;

        internal DeepSwarmApi.Server.EntityMove UpcomingMove = DeepSwarmApi.Server.EntityMove.Idle;

        public int OmniViewRadius = 2;
        public int DirectionalViewRadius = 8;
        public float HalfFieldOfView = MathF.PI / 3f;

        public InternalEntity(int id, InternalWorld world, Point position, DeepSwarmApi.Server.EntityDirection direction, int playerIndex)
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
            DeepSwarmApi.Server.EntityDirection.Right => 0f,
            DeepSwarmApi.Server.EntityDirection.Down => MathF.PI / 2f,
            DeepSwarmApi.Server.EntityDirection.Left => MathF.PI,
            DeepSwarmApi.Server.EntityDirection.Up => MathF.PI * 3f / 2f,
            _ => throw new NotSupportedException(),
        };

        #region API
        #endregion
    }
}
