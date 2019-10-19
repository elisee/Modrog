using SwarmBasics.Math;

namespace ModrogServer.Game
{
    public class InternalEntityKind : ModrogApi.Server.EntityKind
    {
        public readonly Point SpriteLocation;

        internal InternalEntityKind(Point spriteLocation)
        {
            SpriteLocation = spriteLocation;
        }

        #region API
        public override void SetManualControlScheme(ModrogApi.Server.ManualControlScheme scheme)
        {
        }

        public override void SetScriptable(bool scriptable)
        {
        }

        public override void SetCapabilities(ModrogApi.Server.EntityCapabilities caps)
        {
        }
        #endregion
    }
}
