using DeepSwarmBasics.Math;

namespace DeepSwarmServer.Game
{
    public class InternalEntityKind : DeepSwarmApi.Server.EntityKind
    {
        public readonly Point SpriteLocation;

        internal InternalEntityKind(Point spriteLocation)
        {
            SpriteLocation = spriteLocation;
        }

        #region API
        public override void SetManualControlScheme(DeepSwarmApi.Server.ManualControlScheme scheme)
        {
        }

        public override void SetScriptable(bool scriptable)
        {
        }

        public override void SetCapabilities(DeepSwarmApi.Server.EntityCapabilities caps)
        {
        }
        #endregion
    }
}
