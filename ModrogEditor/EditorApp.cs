using SDL2;
using SwarmBasics.Math;
using SwarmCore;
using SwarmPlatform.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ModrogEditor
{
    partial class EditorApp
    {
        // Threading
        readonly int _threadId;
        readonly ThreadActionQueue _actionQueue;

        // State
        public readonly EditorState State;

        // Window
        static readonly Point MinimumWindowSize = new Point(1280, 720);
        readonly IntPtr _window;
        Rectangle _viewport;
        SDL.SDL_EventFilter _watchResizeEventDelegate;

        // Rendering
        readonly IntPtr _renderer;

        // Assets
        readonly string _assetsPath;
        public readonly Font TitleFont;
        public readonly Font HeaderFont;
        public readonly FontStyle HeaderFontStyle;
        public readonly Font MainFont;
        public readonly Font MonoFont;

        // Interface
        public readonly Desktop Desktop;

        public readonly Interface.HomeView HomeView;
        public readonly Interface.Editing.EditingView EditingView;

        public EditorApp()
        {
            // Threading
            _threadId = Thread.CurrentThread.ManagedThreadId;
            _actionQueue = new ThreadActionQueue(_threadId);

            // State
            State = new EditorState(this);

            // Window & Rendering
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_CreateWindowAndRenderer(MinimumWindowSize.X, MinimumWindowSize.Y, SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE, out _window, out _renderer);
            SDL.SDL_SetWindowTitle(_window, "Modrog Editor");
            SDL.SDL_SetWindowMinimumSize(_window, MinimumWindowSize.X, MinimumWindowSize.Y);

            SDL.SDL_GetWindowSize(_window, out var initialWidth, out var initialHeight);
            _viewport = new Rectangle(0, 0, initialWidth, initialHeight);

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
            EditingView = new Interface.Editing.EditingView(this);

            Desktop.RootElement.Layout(_viewport);
            OnStageChanged();
        }

        public void Run()
        {
            var stopwatch = Stopwatch.StartNew();

            _watchResizeEventDelegate = WatchResizeEvent;
            SDL.SDL_AddEventWatch(_watchResizeEventDelegate, IntPtr.Zero);

            while (State.Stage != EditorStage.Exited)
            {
                // Input
                while (State.Stage != EditorStage.Exited && SDL.SDL_PollEvent(out var @event) != 0)
                {
                    switch (@event.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            State.Stop();
                            break;

                        case SDL.SDL_EventType.SDL_WINDOWEVENT:
                            if (@event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE) State.Stop();
                            else Desktop.HandleSDLEvent(@event);
                            break;

                        default:
                            Desktop.HandleSDLEvent(@event);
                            break;
                    }
                }

                if (State.Stage == EditorStage.Exited) break;

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
                case EditorStage.Home: Desktop.RootElement.Add(HomeView); break;
                case EditorStage.Editing: Desktop.RootElement.Add(EditingView); break;
            }

            Desktop.RootElement.Layout(_viewport);
        }
    }
}