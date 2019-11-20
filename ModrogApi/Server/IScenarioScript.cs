namespace ModrogApi.Server
{
    public interface IScenarioScript
    {
        void OnEntityIntent(Entity entity, EntityIntent intent, Direction direction, int slot, out bool preventDefault);
        void Tick();
    }
}
