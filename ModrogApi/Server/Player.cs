using SwarmBasics.Math;

namespace ModrogApi.Server
{
    public abstract class Player
    {
        public abstract void Teleport(World world, Point position);
        public abstract void ShowTip(string tip);
    }
}