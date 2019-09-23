using DeepSwarmClient.UI;
using DeepSwarmCommon;

namespace DeepSwarmClient
{
    class EngineElement : Element
    {
        public readonly Engine Engine;

        public EngineElement(Engine engine, Element parent) : base(engine.Desktop, parent)
        {
            Engine = engine;
        }

        protected Rectangle MakeCenteredRectangle(int width, int height)
        {
            return new Rectangle(Engine.Viewport.Width / 2 - width / 2, Engine.Viewport.Height / 2 - height / 2, width, height);
        }

    }
}
