using DeepSwarmBasics.Math;
using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.UI;
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

        public readonly HomeView HomeView;
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
            PopupLayer = new Panel(Desktop.RootElement, new TexturePatch(0x00000088)) { IsVisible = false };

            HomeView = new HomeView(this);
            LoadingView = new LoadingView(this);
            LobbyView = new LobbyView(this);
            PlayingView = new Playing.PlayingView(this);

            Desktop.RootElement.Layout(Viewport);
            OnStageChanged();
        }

        public void SetViewport(Rectangle viewport)
        {
            Viewport = viewport;
            Desktop.RootElement.Layout(Viewport);
        }

        public void OnStageChanged()
        {
            Desktop.SetFocusedElement(null);
            ViewLayer.Clear();

            switch (Engine.State.Stage)
            {
                case ClientStage.Home: ViewLayer.Add(HomeView); break;
                case ClientStage.Loading: ViewLayer.Add(LoadingView); break;
                case ClientStage.Lobby: ViewLayer.Add(LobbyView); break;
                case ClientStage.Playing: ViewLayer.Add(PlayingView); break;
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
