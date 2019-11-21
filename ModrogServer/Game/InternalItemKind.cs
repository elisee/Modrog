using SwarmBasics.Math;

namespace ModrogServer.Game
{
    sealed class InternalItemKind : ModrogApi.Server.ItemKind
    {
        public readonly Point SpriteLocation;

        internal InternalItemKind(Point spriteLocation)
        {
            SpriteLocation = spriteLocation;
        }

        #region API
        #endregion
    }
}
