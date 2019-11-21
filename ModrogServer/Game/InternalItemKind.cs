using SwarmBasics.Math;

namespace ModrogServer.Game
{
    sealed class InternalItemKind : ModrogApi.Server.ItemKind
    {
        public readonly int Id;
        public readonly Point SpriteLocation;

        internal InternalItemKind(int id, Point spriteLocation)
        {
            Id = id;
            SpriteLocation = spriteLocation;
        }

        #region API
        #endregion
    }
}
