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
                AnchorRectangle = MakeCenteredRectangle(320, 320),
                BackgroundColor = new Color(0x88aa88ff)
            };

            new Label(Desktop, loadingPopup) { Text = "Loading" };
        }
    }
}
