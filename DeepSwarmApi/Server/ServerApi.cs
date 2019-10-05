using DeepSwarmBasics.Math;

namespace DeepSwarmApi.Server
{
    public sealed class ServerApi
    {
        public readonly Player[] Players;

        ServerApi(Player[] players)
        {
            Players = players;
        }

        public Tile CreateTile(Point spriteLocation)
        {
            return new Tile();
        }

        public World CreateWorld(int width, int height)
        {
            return new World();
        }

        public EntityKind CreateEntityKind(Point spriteLocation)
        {
            return new EntityKind();
        }
    }
}
