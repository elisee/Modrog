using SwarmBasics.Math;

namespace ModrogApi.Server
{
    public abstract class Universe
    {
        public abstract void SetSpritesheet(string path);
        public abstract void SetTileSize(int tileSize);
        public abstract TileKind CreateTileKind(MapLayer layer, Point spriteLocation);
        public abstract CharacterKind CreateCharacterKind(Point spriteLocation);
        public abstract ItemKind CreateItemKind(Point spriteLocation);
        public abstract World CreateWorld();

        public abstract Map LoadMap(string path);
        public abstract void LoadTileSet(string path);

        public abstract Player[] GetPlayers();
    }
}
