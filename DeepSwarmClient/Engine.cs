using DeepSwarmClient.UI;
using DeepSwarmCommon;
using SDL2;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
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
        public readonly EngineState State;
        bool _isRunning;

        // Paths
        public readonly string SettingsFilePath;
        public readonly string AssetsPath;
        public readonly string ScriptsPath;

        // Assets
        public readonly IntPtr SpritesheetTexture;

        // Networking
        Socket _socket;
        PacketReceiver _packetReceiver;
        public readonly PacketWriter PacketWriter = new PacketWriter();
        public readonly PacketReader PacketReader = new PacketReader();

        // UI
        public readonly Desktop Desktop;
        public readonly EnterNameView EnterNameView;
        public readonly LoadingView LoadingView;
        public readonly InGameView InGameView;

        public Engine()
        {
            // Rendering
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_CreateWindowAndRenderer(1280, 720, 0, out Window, out Renderer);

            // State
            State = new EngineState(this);

            // Identity
            var identityPath = Path.Combine(AppContext.BaseDirectory, "Identity.dat");

            if (File.Exists(identityPath))
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

            // UI
            Desktop = new Desktop(Renderer);
            EnterNameView = new EnterNameView(this);
            LoadingView = new LoadingView(this);
            InGameView = new InGameView(this);
        }

        public void Start()
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true, LingerState = new LingerOption(true, seconds: 1) };
            _socket.Connect(new IPEndPoint(IPAddress.Loopback, Protocol.Port));
            _packetReceiver = new PacketReceiver(_socket);

            Desktop.SetRootElement(EnterNameView);
            EnterNameView.NameInput.SetValue(State.SelfPlayerName ?? "");
            Desktop.SetFocusedElement(EnterNameView.NameInput);

            Run();
        }

        void Run()
        {
            _isRunning = true;
            var stopwatch = Stopwatch.StartNew();

            while (_isRunning)
            {
                // Network
                if (_socket.Poll(0, SelectMode.SelectRead))
                {
                    if (!_packetReceiver.Read(out var packets))
                    {
                        Trace.WriteLine($"Disconnected from server.");
                        // isRunning = false;
                        break;
                    }

                    ReadPackets(packets);
                }

                // Input
                while (_isRunning && SDL.SDL_PollEvent(out var @event) != 0)
                {
                    switch (@event.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            _isRunning = false;
                            break;

                        case SDL.SDL_EventType.SDL_WINDOWEVENT:
                            switch (@event.window.windowEvent)
                            {
                                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                                    _isRunning = false;
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
                            Desktop.HandleSDLEvent(@event);
                            break;
                    }
                }

                if (!_isRunning) break;

                var deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();

                Update(deltaTime);

                // Render
                SDL.SDL_SetRenderDrawColor(Renderer, 0, 0, 0, 255);
                SDL.SDL_RenderClear(Renderer);

                Desktop.Draw();

                SDL.SDL_RenderPresent(Renderer);

                Thread.Sleep(1);
            }

            _socket.Close();

            SDL_image.IMG_Quit();
            SDL.SDL_Quit();
        }

        void Update(float deltaTime)
        {
            Desktop.Animate(deltaTime);
        }

        public void SendPacket()
        {
            try { _socket.Send(PacketWriter.Buffer, 0, PacketWriter.Finish(), SocketFlags.None); } catch { }
        }
    }
}