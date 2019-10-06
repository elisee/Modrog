using DeepSwarmBasics.Math;

namespace DeepSwarmApi.Server
{
    public abstract class World
    {
        public abstract Entity SpawnEntity(EntityKind kind, Point position, EntityDirection direction, Player owner);
        public abstract void SetTile(int x, int y, TileKind tileKind);
    }
}