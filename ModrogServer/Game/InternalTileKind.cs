using SwarmBasics.Math;

namespace ModrogServer.Game
{
    public class InternalTileKind : ModrogApi.Server.TileKind
    {
        public readonly short Index;
        internal readonly Point SpriteLocation;
        internal readonly ModrogApi.Server.TileFlags Flags;

        internal InternalTileKind(short index, Point spriteLocation, ModrogApi.Server.TileFlags flags)
        {
            Index = index;
            SpriteLocation = spriteLocation;
            Flags = flags;
        }
    }
}
