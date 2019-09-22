using DeepSwarmClient.UI;

namespace DeepSwarmClient
{
    class LoadingView : EngineElement
    {
        public LoadingView(Engine engine)
            : base(engine, null)
        {
            AnchorRectangle = engine.Viewport;

            var loadingPopup = new Element(Desktop, this)
            {
                AnchorRectangle = new Rectangle(16, 16, 320 + 32, 320 + 32),
                BackgroundColor = new Color(0x88aa88ff)
            };

            new Label(Desktop, loadingPopup) { Text = "Loading" };
        }
    }
}
