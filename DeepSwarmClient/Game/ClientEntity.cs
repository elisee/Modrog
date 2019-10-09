using DeepSwarmApi;
using DeepSwarmBasics.Math;

namespace DeepSwarmClient.Game
{
    public class ClientEntity
    {
        public readonly int Id;

        public Point SpriteLocation;
        public Point Position;
        public EntityDirection Direction;
        public int PlayerIndex;

        public ClientEntity(int id)
        {
            Id = id;
        }

        internal EntityMove GetMoveForTargetDirection(EntityDirection targetDirection)
        {
            if (targetDirection == Direction) return EntityMove.Forward;

            var diff = (Direction - targetDirection + 4) % 4 - 2;
            if (diff < 0) return EntityMove.RotateCCW;
            else return EntityMove.RotateCW;
        }
    }
}
