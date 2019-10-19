using SwarmBasics.Math;
using SwarmCore;
using SDL2;
using System;
using System.Diagnostics;
using System.Threading;

namespace ModrogEditor
{
    partial class Engine
    {
        // Threading
        readonly int _threadId;
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
            _threadId = Thread.CurrentThread.ManagedThreadId;
            _actionQueue = new ThreadActionQueue(_threadId);

            // Rendering
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_CreateWindowAndRenderer(MinimumWindowSize.X, MinimumWindowSize.Y, SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE, out Window, out Renderer);
            SDL.SDL_SetWindowTitle(Window, "Modrog Editor");
            SDL.SDL_SetWindowMinimumSize(Window, MinimumWindowSize.X, MinimumWindowSize.Y);

            // State
            State = new EditorState(this);

            // Assets
            AssetsPath = FileHelper.FindAppFolder("Assets");
            if (SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG) != (int)SDL_image.IMG_InitFlags.IMG_INIT_PNG) throw new Exception();

            // Interface
            SDL.SDL_GetWindowSize(Window, out var initialWidth, out var initialHeight);
            Interface = new Interface.Interface(this, new Rectangle(0, 0, initialWidth, initialHeight));
        }

        public void Start()
        {
            Run();
        }

        public void RunOnEngineThread(Action action) => _actionQueue.Run(action);

        void Run()
        {
            var stopwatch = Stopwatch.StartNew();

            // Handle resize events live rather than when the resize operation is over
            SDL.SDL_SetEventFilter((userData, eventPtr) =>
            {
                Debug.Assert(Thread.CurrentThread.ManagedThreadId == _threadId);

                unsafe
                {
                    var @event = *(SDL.SDL_Event*)eventPtr;

                    if (@event.type == SDL.SDL_EventType.SDL_WINDOWEVENT && @event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                    {
                        Interface.SetViewport(new Rectangle(0, 0, @event.window.data1, @event.window.data2));
                        Draw();
                    }
                }

                return 1;
            }, IntPtr.Zero);

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

                State.Update(deltaTime);
                Interface.Desktop.Animate(deltaTime);

                _actionQueue.ExecuteActions();

                Draw();

                Thread.Sleep(1);
            }

            SDL_image.IMG_Quit();
            SDL.SDL_Quit();

            void Draw()
            {
                SDL.SDL_SetRenderDrawColor(Renderer, 0, 0, 0, 255);
                SDL.SDL_RenderClear(Renderer);

                Interface.Desktop.Draw();

                SDL.SDL_RenderPresent(Renderer);
            }
        }
    }
}