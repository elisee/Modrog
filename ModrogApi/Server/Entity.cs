using SwarmBasics.Math;

namespace ModrogApi.Server
{
    public abstract class Entity
    {
        public object Custom;

        public abstract World GetWorld();
        public abstract Point GetPosition();

        public abstract void SetView(int viewRadius);
        public abstract void Teleport(World world, Point position);
        public abstract void Remove();

        // Character
        public abstract ItemKind GetItem(int index);
        public abstract void SetItem(int index, ItemKind itemKind);
    }
}
