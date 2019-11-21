using SwarmBasics.Math;

namespace ModrogServer.Game
{
    sealed class InternalCharacterKind : ModrogApi.Server.CharacterKind
    {
        public readonly Point SpriteLocation;

        internal InternalCharacterKind(Point spriteLocation)
        {
            SpriteLocation = spriteLocation;
        }

        #region API
        #endregion
    }
}
