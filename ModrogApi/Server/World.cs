using SwarmBasics.Math;

namespace ModrogApi.Server
{
    public abstract class World
    {
        public abstract Entity SpawnEntity(EntityKind kind, Point position, Player owner);
        public abstract void SetTile(MapLayer layer, int x, int y, TileKind tileKind);

        public abstract void InsertMap(int x, int y, Map map);
    }
}
