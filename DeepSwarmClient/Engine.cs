using DeepSwarmCommon;
using SDL2;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace DeepSwarmClient
{
    partial class Engine
    {
        // Rendering
        public readonly Rectangle Viewport = new Rectangle(0, 0, 1280, 720);
        public readonly IntPtr Window;
        public readonly IntPtr Renderer;

        // State
        public readonly ClientState State;

        // Paths
        public readonly string SettingsFilePath;
        public readonly string AssetsPath;
        public readonly string ScriptsPath;

        // Assets
        public readonly IntPtr SpritesheetTexture;

        // Interface
        public readonly Interface.Interface Interface;

        public Engine(bool newIdentity)
        {
            // Rendering
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_CreateWindowAndRenderer(Viewport.Width, Viewport.Height, 0, out Window, out Renderer);
            SDL.SDL_SetWindowTitle(Window, "DeepSwarm");

            // State
            State = new ClientState(this);

            // Identity
            var identityPath = Path.Combine(AppContext.BaseDirectory, "Identity.dat");

            if (!newIdentity && File.Exists(identityPath))
            {
                try { State.SelfGuid = new Guid(File.ReadAllBytes(identityPath)); } catch { }
            }

            if (State.SelfGuid == Guid.Empty)
            {
                State.SelfGuid = Guid.NewGuid();
                File.WriteAllBytes(identityPath, State.SelfGuid.ToByteArray());
            }

            // Settings
            SettingsFilePath = Path.Combine(AppContext.BaseDirectory, "Settings.txt");
            try { State.SelfPlayerName = File.ReadAllText(SettingsFilePath); } catch { }

            // Scripts
            ScriptsPath = Path.Combine(AppContext.BaseDirectory, "Scripts");
            if (!Directory.Exists(ScriptsPath)) Directory.CreateDirectory(ScriptsPath);

            foreach (var scriptFilePath in Directory.EnumerateFiles(ScriptsPath, "*.lua", SearchOption.AllDirectories))
            {
                var relativeFilePath = scriptFilePath.Substring(ScriptsPath.Length + 1);
                State.Scripts.Add(relativeFilePath, File.ReadAllText(scriptFilePath));
            }

            // Assets
            AssetsPath = FileHelper.FindAppFolder("Assets");
            if (SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG) != (int)SDL_image.IMG_InitFlags.IMG_INIT_PNG) throw new Exception();
            RendererHelper.FontTexture = SDL_image.IMG_LoadTexture(Renderer, Path.Combine(AssetsPath, "Font.png"));
            SpritesheetTexture = SDL_image.IMG_LoadTexture(Renderer, Path.Combine(AssetsPath, "Spritesheet.png"));

            // Interface
            Interface = new Interface.Interface(this);
        }

        public void Start()
        {
            Run();
        }

        void Run()
        {
            var stopwatch = Stopwatch.StartNew();

            while (State.IsRunning)
            {
                // Input
                while (State.IsRunning && SDL.SDL_PollEvent(out var @event) != 0)
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

                if (!State.IsRunning) break;

                var deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();

                Update(deltaTime);

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