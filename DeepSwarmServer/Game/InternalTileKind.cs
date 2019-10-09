using DeepSwarmBasics.Math;

namespace DeepSwarmServer.Game
{
    public class InternalTileKind : DeepSwarmApi.Server.TileKind
    {
        public readonly short Index;
        internal readonly Point SpriteLocation;
        internal readonly DeepSwarmApi.Server.TileFlags Flags;

        internal InternalTileKind(short index, Point spriteLocation, DeepSwarmApi.Server.TileFlags flags)
        {
            Index = index;
            SpriteLocation = spriteLocation;
            Flags = flags;
        }
    }
}
