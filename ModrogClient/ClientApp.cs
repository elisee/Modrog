using SDL2;
using SwarmBasics.Math;
using SwarmCore;
using SwarmPlatform.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ModrogClient
{
    class ClientApp
    {
        // Threading
        readonly int _threadId;
        readonly ThreadActionQueue _actionQueue;

        // State
        public readonly ClientState State;

        // Window
        static readonly Point MinimumWindowSize = new Point(1280, 720);
        readonly IntPtr _window;
        Rectangle _viewport;
        SDL.SDL_EventFilter _watchResizeEventDelegate;

        // Rendering
        readonly IntPtr _renderer;

        // Assets
        readonly string _assetsPath;
        readonly string _scriptsPath;

        // Interface
        public readonly Font TitleFont;
        public readonly Font HeaderFont;
        public readonly FontStyle HeaderFontStyle;
        public readonly Font MainFont;
        public readonly Font MonoFont;

        public readonly Desktop Desktop;

        public readonly Interface.HomeView HomeView;
        public readonly Interface.LoadingView LoadingView;
        public readonly Interface.LobbyView LobbyView;
        public readonly Interface.Playing.PlayingView PlayingView;

        public ClientApp()
        {
            // Threading
            _threadId = Thread.CurrentThread.ManagedThreadId;
            _actionQueue = new ThreadActionQueue(_threadId);

            // State
            State = new ClientState(this);

            // Window & Rendering
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_CreateWindowAndRenderer(MinimumWindowSize.X, MinimumWindowSize.Y, 0, out _window, out _renderer);
            SDL.SDL_SetWindowTitle(_window, "Modrog");
            SDL.SDL_SetWindowResizable(_window, SDL.SDL_bool.SDL_TRUE);
            SDL.SDL_SetWindowMinimumSize(_window, MinimumWindowSize.X, MinimumWindowSize.Y);

            SDL.SDL_GetWindowSize(_window, out var initialWidth, out var initialHeight);
            _viewport = new Rectangle(0, 0, initialWidth, initialHeight);

            // Scripts
            _scriptsPath = Path.Combine(AppContext.BaseDirectory, "Scripts");
            if (!Directory.Exists(_scriptsPath)) Directory.CreateDirectory(_scriptsPath);

            foreach (var scriptFilePath in Directory.EnumerateFiles(_scriptsPath, "*.lua", SearchOption.AllDirectories))
            {
                var relativeFilePath = scriptFilePath.Substring(_scriptsPath.Length + 1);
                State.Scripts.Add(relativeFilePath, File.ReadAllText(scriptFilePath));
            }

            // Assets
            _assetsPath = FileHelper.FindAppFolder("Assets");
            if (SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG) != (int)SDL_image.IMG_InitFlags.IMG_INIT_PNG) throw new Exception();

            TitleFont = Font.LoadFromChevyRayFolder(_renderer, Path.Combine(_assetsPath, "Fonts", "ChevyRay - Roundabout"));
            HeaderFont = Font.LoadFromChevyRayFolder(_renderer, Path.Combine(_assetsPath, "Fonts", "ChevyRay - Skullboy"));
            HeaderFontStyle = new FontStyle(HeaderFont) { Scale = 2, LetterSpacing = 1, LineSpacing = 8 };

            MainFont = Font.LoadFromChevyRayFolder(_renderer, Path.Combine(_assetsPath, "Fonts", "ChevyRay - Softsquare"));
            MonoFont = Font.LoadFromChevyRayFolder(_renderer, Path.Combine(_assetsPath, "Fonts", "ChevyRay - Softsquare Mono"));

            // Interface
            Desktop = new Desktop(_renderer,
                mainFontStyle: new FontStyle(MainFont) { Scale = 2, LetterSpacing = 1, LineSpacing = 8 },
                monoFontStyle: new FontStyle(MonoFont) { Scale = 2, LetterSpacing = 1, LineSpacing = 8 });

            HomeView = new Interface.HomeView(this);
            LoadingView = new Interface.LoadingView(this);
            LobbyView = new Interface.LobbyView(this);
            PlayingView = new Interface.Playing.PlayingView(this);

            Desktop.RootElement.Layout(_viewport);
            OnStageChanged();
        }

        public void Run()
        {
            var stopwatch = Stopwatch.StartNew();

            _watchResizeEventDelegate = WatchResizeEvent;
            SDL.SDL_AddEventWatch(_watchResizeEventDelegate, IntPtr.Zero);

            while (State.Stage != ClientStage.Exited)
            {
                while (State.Stage != ClientStage.Exited && SDL.SDL_PollEvent(out var @event) != 0)
                {
                    switch (@event.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            State.Stop();
                            break;

                        case SDL.SDL_EventType.SDL_WINDOWEVENT:
                            if (@event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE) State.Stop();
                            break;

                        default:
                            Desktop.HandleSDLEvent(@event);
                            break;
                    }
                }

                if (State.Stage == ClientStage.Exited) break;

                var deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();

                State.Update(deltaTime);
                Desktop.Animate(deltaTime);
                _actionQueue.ExecuteActions();

                Draw();

                Thread.Sleep(1);
            }

            SDL.SDL_DelEventWatch(_watchResizeEventDelegate, IntPtr.Zero);

            SDL_image.IMG_Quit();
            SDL.SDL_Quit();
        }

        public void RunOnAppThread(Action action) => _actionQueue.Run(action);

        void Draw()
        {
            SDL.SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);
            SDL.SDL_RenderClear(_renderer);

            Desktop.Draw();

            SDL.SDL_RenderPresent(_renderer);
        }

        int WatchResizeEvent(IntPtr userData, IntPtr eventPtr)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == _threadId);

            unsafe
            {
                var @event = *(SDL.SDL_Event*)eventPtr;

                if (@event.type == SDL.SDL_EventType.SDL_WINDOWEVENT && @event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                {
                    _viewport = new Rectangle(0, 0, @event.window.data1, @event.window.data2);
                    Desktop.RootElement.Layout(_viewport);
                    Draw();
                }
            }

            return 1;
        }

        public void OnStageChanged()
        {
            Desktop.SetFocusedElement(null);
            Desktop.RootElement.Clear();

            switch (State.Stage)
            {
                case ClientStage.Home: Desktop.RootElement.Add(HomeView); break;
                case ClientStage.Loading: Desktop.RootElement.Add(LoadingView); break;
                case ClientStage.Lobby: Desktop.RootElement.Add(LobbyView); break;
                case ClientStage.Playing: Desktop.RootElement.Add(PlayingView); break;
            }

            Desktop.RootElement.Layout(_viewport);
        }
    }
}