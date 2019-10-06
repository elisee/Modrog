namespace DeepSwarmApi.Server
{
    public abstract class EntityKind
    {
        public abstract void SetManualControlScheme(ManualControlScheme scheme);
        public abstract void SetScriptable(bool scriptable);
        public abstract void SetCapabilities(EntityCapabilities caps);
    }
}