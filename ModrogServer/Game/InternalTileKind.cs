using SwarmBasics.Math;

namespace ModrogServer.Game
{
    sealed class InternalTileKind : ModrogApi.Server.TileKind
    {
        public readonly short Index;
        internal readonly Point SpriteLocation;

        internal InternalTileKind(short index, Point spriteLocation)
        {
            Index = index;
            SpriteLocation = spriteLocation;
        }
    }
}
