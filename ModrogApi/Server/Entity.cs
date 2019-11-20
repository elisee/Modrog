using SwarmBasics.Math;

namespace ModrogApi.Server
{
    public abstract class Entity
    {
        public abstract World GetWorld();
        public abstract Point GetPosition();

        public abstract void SetView(int viewRadius);
        public abstract void Teleport(World world, Point position);

        public abstract ItemKind GetStorageSlotItem(int index);
        public abstract void SetStorageSlotItem(int index, ItemKind itemKind);

        public abstract ItemKind GetActionSlotItem(int index);
        public abstract void SetActionSlotItem(int index, ItemKind itemKind);
    }
}
