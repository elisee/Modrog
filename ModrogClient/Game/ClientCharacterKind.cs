using SwarmBasics.Math;

namespace ModrogClient.Game
{
    class ClientCharacterKind
    {
        public readonly int Id;
        public readonly Point SpriteLocation;

        public ClientCharacterKind(int id, Point spriteLocation)
        {
            Id = id;
            SpriteLocation = spriteLocation;
        }
    }
}
