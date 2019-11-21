namespace ModrogApi.Server
{
    public interface IScenarioScript
    {
        void OnCharacterIntent(Entity entity, CharacterIntent intent, Direction direction, int slot, out bool preventDefault);
        void Tick();
    }
}
