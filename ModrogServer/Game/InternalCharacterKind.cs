using SwarmBasics.Math;

namespace ModrogServer.Game
{
    sealed class InternalCharacterKind : ModrogApi.Server.CharacterKind
    {
        public readonly int Id;
        public readonly Point SpriteLocation;
        public readonly int Health;

        internal InternalCharacterKind(int id, Point spriteLocation, int health)
        {
            Id = id;
            SpriteLocation = spriteLocation;
            Health = health;
        }

        #region API
        #endregion
    }
}
