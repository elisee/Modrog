using DeepSwarmBasics.Math;
using DeepSwarmClient.Graphics;
using DeepSwarmClient.UI;
using System.IO;

namespace DeepSwarmClient.Interface
{
    class Interface
    {
        public readonly Engine Engine;
        public Rectangle Viewport { get; private set; }

        public readonly Font TitleFont;
        public readonly Font HeaderFont;
        public readonly FontStyle HeaderFontStyle;
        public readonly Font MainFont;
        public readonly Font MonoFont;

        public readonly Desktop Desktop;
        public readonly Panel ViewLayer;
        public readonly Panel PopupLayer;

        public readonly ConnectView ConnectView;
        public readonly LoadingView LoadingView;
        public readonly LobbyView LobbyView;
        public readonly Playing.PlayingView PlayingView;

        public Interface(Engine engine, Rectangle viewport)
        {
            Engine = engine;
            Viewport = viewport;

            TitleFont = Font.LoadFromChevyRayFolder(Engine.Renderer, Path.Combine(Engine.AssetsPath, "Fonts", "ChevyRay - Roundabout"));
            HeaderFont = Font.LoadFromChevyRayFolder(Engine.Renderer, Path.Combine(Engine.AssetsPath, "Fonts", "ChevyRay - Skullboy"));
            HeaderFontStyle = new FontStyle(HeaderFont) { Scale = 2, LetterSpacing = 1, LineSpacing = 8 };

            MainFont = Font.LoadFromChevyRayFolder(Engine.Renderer, Path.Combine(Engine.AssetsPath, "Fonts", "ChevyRay - Softsquare"));
            MonoFont = Font.LoadFromChevyRayFolder(Engine.Renderer, Path.Combine(Engine.AssetsPath, "Fonts", "ChevyRay - Softsquare Mono"));

            Desktop = new Desktop(engine.Renderer,
                mainFontStyle: new FontStyle(MainFont) { Scale = 2, LetterSpacing = 1, LineSpacing = 8 },
                monoFontStyle: new FontStyle(MonoFont) { Scale = 2, LetterSpacing = 1, LineSpacing = 8 });

            ViewLayer = new Panel(Desktop.RootElement, new TexturePatch(0x000000ff));
            PopupLayer = new Panel(Desktop.RootElement, new TexturePatch(0x00000066)) { IsVisible = false };

            ConnectView = new ConnectView(this);
            LoadingView = new LoadingView(this);
            LobbyView = new LobbyView(this);
            PlayingView = new Playing.PlayingView(this);

            Desktop.RootElement.Layout(Viewport);
            OnViewChanged();
        }

        public void SetViewport(Rectangle viewport)
        {
            Viewport = viewport;
            Desktop.RootElement.Layout(Viewport);
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
