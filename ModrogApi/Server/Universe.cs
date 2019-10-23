using SwarmBasics.Math;

namespace ModrogApi.Server
{
    public abstract class Universe
    {
        public abstract void SetSpritesheet(string path);

        public abstract Player[] GetPlayers();
        public abstract EntityKind CreateEntityKind(Point spriteLocation);
        public abstract TileKind CreateTileKind(MapLayer layer, Point spriteLocation);
        public abstract World CreateWorld();

        public abstract Map LoadMap(string path);
        public abstract void LoadTileSet(string path);
    }
}
