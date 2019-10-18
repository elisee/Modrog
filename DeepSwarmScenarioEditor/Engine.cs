using DeepSwarmBasics.Math;
using DeepSwarmCommon;
using SDL2;
using System;
using System.Diagnostics;
using System.Threading;

namespace DeepSwarmScenarioEditor
{
    partial class Engine
    {
        // Threading
        readonly ThreadActionQueue _actionQueue;

        // Rendering
        static readonly Point MinimumWindowSize = new Point(1280, 720);
        public readonly IntPtr Window;

        public readonly IntPtr Renderer;

        // State
        public readonly EditorState State;

        // Paths
        public readonly string AssetsPath;

        // Interface
        public readonly Interface.Interface Interface;

        public Engine()
        {
            _actionQueue = new ThreadActionQueue(Thread.CurrentThread.ManagedThreadId);

            // Rendering
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_CreateWindowAndRenderer(MinimumWindowSize.X, MinimumWindowSize.Y, SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED, out Window, out Renderer);
            SDL.SDL_SetWindowTitle(Window, "DeepSwarm - Editor");
            SDL.SDL_SetWindowResizable(Window, SDL.SDL_bool.SDL_TRUE);
            SDL.SDL_SetWindowMinimumSize(Window, MinimumWindowSize.X, MinimumWindowSize.Y);

            // State
            State = new EditorState(this);

            // Assets
            AssetsPath = FileHelper.FindAppFolder("Assets");
            if (SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG) != (int)SDL_image.IMG_InitFlags.IMG_INIT_PNG) throw new Exception();

            // Interface
            Interface = new Interface.Interface(this, new Rectangle(0, 0, MinimumWindowSize.X, MinimumWindowSize.Y));
        }

        public void Start()
        {
            Run();
        }

        public void RunOnEngineThread(Action action) => _actionQueue.Run(action);

        void Run()
        {
            var stopwatch = Stopwatch.StartNew();

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
                            switch (@event.window.windowEvent)
                            {
                                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                                    State.Stop();
                                    break;

                                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                                    Interface.SetViewport(new Rectangle(0, 0, @event.window.data1, @event.window.data2));
                                    break;
                            }

                            break;

                        case SDL.SDL_EventType.SDL_KEYDOWN:
                        case SDL.SDL_EventType.SDL_KEYUP:
                        case SDL.SDL_EventType.SDL_TEXTINPUT:
                        case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                        case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                        case SDL.SDL_EventType.SDL_MOUSEMOTION:
                        case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                            Interface.Desktop.HandleSDLEvent(@event);
                            break;
                    }
                }

                if (State.Stage == EditorStage.Exited) break;

                var deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();

                Update(deltaTime);
                _actionQueue.ExecuteActions();

                // Render
                SDL.SDL_SetRenderDrawColor(Renderer, 0, 0, 0, 255);
                SDL.SDL_RenderClear(Renderer);

                Interface.Desktop.Draw();

                SDL.SDL_RenderPresent(Renderer);

                Thread.Sleep(1);
            }

            SDL_image.IMG_Quit();
            SDL.SDL_Quit();
        }

        void Update(float deltaTime)
        {
            State.Update(deltaTime);
            Interface.Desktop.Animate(deltaTime);
        }
    }
}