using SwarmBasics.Math;

namespace ModrogClient.Game
{
    class ClientCharacterKind
    {
        public readonly int Id;
        public readonly Point SpriteLocation;
        public readonly int Health;

        public ClientCharacterKind(int id, Point spriteLocation, int health)
        {
            Id = id;
            SpriteLocation = spriteLocation;
            Health = health;
        }
    }
}
