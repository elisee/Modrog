using DeepSwarmClient.UI;

namespace DeepSwarmClient
{
    class EngineElement : Element
    {
        public readonly Engine Engine;

        public EngineElement(Engine engine, Element parent) : base(engine.Desktop, parent)
        {
            Engine = engine;
        }
    }
}
