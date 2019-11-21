using SwarmBasics.Math;

namespace ModrogClient.Game
{
    class ClientItemKind
    {
        public readonly int Id;
        public readonly Point SpriteLocation;

        public ClientItemKind(int id, Point spriteLocation)
        {
            Id = id;
            SpriteLocation = spriteLocation;
        }
    }
}
