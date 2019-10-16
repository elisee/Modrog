using DeepSwarmBasics.Math;

namespace DeepSwarmApi.Server
{
    public abstract class Universe
    {
        public abstract void SetSpritesheet(string path);

        public abstract Player[] GetPlayers();
        public abstract EntityKind CreateEntityKind(Point spriteLocation);
        public abstract TileKind CreateTileKind(Point spriteLocation, TileFlags flags);
        public abstract World CreateWorld(int width, int height);
    }
}
