using DeepSwarmApi;
using DeepSwarmBasics.Math;

namespace DeepSwarmClient.Game
{
    public class ClientEntity
    {
        public readonly int Id;
        public Point Position;
        public EntityDirection Direction;
        public int PlayerIndex;

        public ClientEntity(int id)
        {
            Id = id;
        }
    }
}
