using SwarmBasics.Math;

namespace ModrogServer.Game
{
    sealed class InternalEntityKind : ModrogApi.Server.EntityKind
    {
        public readonly Point SpriteLocation;

        internal InternalEntityKind(Point spriteLocation)
        {
            SpriteLocation = spriteLocation;
        }

        #region API
        #endregion
    }
}
