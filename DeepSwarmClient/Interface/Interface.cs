using DeepSwarmClient.UI;

namespace DeepSwarmClient.Interface
{
    class Interface
    {
        public readonly Engine Engine;

        public readonly Desktop Desktop;
        public readonly Panel ViewLayer;
        public readonly Panel PopupLayer;

        public readonly ConnectView ConnectView;
        public readonly EnterNameView EnterNameView;
        public readonly LoadingView LoadingView;
        public readonly Playing.PlayingView PlayingView;

        public Interface(Engine engine)
        {
            Engine = engine;

            Desktop = new Desktop(engine.Renderer);
            ViewLayer = new Panel(Desktop, Desktop.RootElement, new Color(0x000000ff));
            PopupLayer = new Panel(Desktop, Desktop.RootElement, new Color(0x00000066)) { IsVisible = false };

            ConnectView = new ConnectView(this);
            EnterNameView = new EnterNameView(this);
            LoadingView = new LoadingView(this);
            PlayingView = new Playing.PlayingView(this);

            Desktop.RootElement.Layout(Engine.Viewport);
            OnViewChanged();
        }

        public void OnViewChanged()
        {
            Desktop.SetFocusedElement(null);
            ViewLayer.Clear();

            switch (Engine.State.View)
            {
                case EngineView.Connect: ViewLayer.Add(ConnectView); break;
                case EngineView.EnterName: ViewLayer.Add(EnterNameView); break;
                case EngineView.Loading: ViewLayer.Add(LoadingView); break;
                case EngineView.Playing: ViewLayer.Add(PlayingView); break;
            }

            ViewLayer.Layout(Desktop.RootElement.LayoutRectangle);
        }
    }
}
