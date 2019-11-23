using SwarmBasics.Math;
using System.Collections.Generic;

namespace ModrogApi.Server
{
    public abstract class World
    {
        public abstract Entity SpawnCharacter(CharacterKind kind, Point position, Player owner);
        public abstract Entity SpawnItem(ItemKind kind, Point position);

        public abstract void SetTile(MapLayer layer, Point position, TileKind tileKind);
        public abstract void InsertMap(int x, int y, Map map);

        public abstract IReadOnlyList<Entity> GetEntities(Point position);

        public abstract void Destroy();
    }
}
