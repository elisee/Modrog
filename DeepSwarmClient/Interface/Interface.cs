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
        public readonly LoadingView LoadingView;
        public readonly LobbyView LobbyView;
        public readonly Playing.PlayingView PlayingView;

        public Interface(Engine engine)
        {
            Engine = engine;

            Desktop = new Desktop(engine.Renderer);
            ViewLayer = new Panel(Desktop, Desktop.RootElement, new TexturePatch(0x000000ff));
            PopupLayer = new Panel(Desktop, Desktop.RootElement, new TexturePatch(0x00000066)) { IsVisible = false };

            ConnectView = new ConnectView(this);
            LoadingView = new LoadingView(this);
            LobbyView = new LobbyView(this);
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
                case EngineView.Loading: ViewLayer.Add(LoadingView); break;
                case EngineView.Lobby: ViewLayer.Add(LobbyView); break;
                case EngineView.Playing: ViewLayer.Add(PlayingView); break;
            }

            ViewLayer.Layout(Desktop.RootElement.LayoutRectangle);
        }

        public void SetPopup(InterfaceElement popup)
        {
            Desktop.SetFocusedElement(null);
            PopupLayer.Clear();
            PopupLayer.IsVisible = popup != null;

            if (popup != null)
            {
                PopupLayer.Add(popup);
                PopupLayer.Layout(Desktop.RootElement.LayoutRectangle);
            }
        }
    }
}
