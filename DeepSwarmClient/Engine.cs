using DeepSwarmClient.UI;
using DeepSwarmCommon;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static DeepSwarmCommon.Protocol;

namespace DeepSwarmClient
{
    partial class Engine
    {
        // Rendering
        public readonly Rectangle Viewport = new Rectangle(0, 0, 1280, 720);
        public readonly IntPtr Window;
        public readonly IntPtr Renderer;

        // Settings
        public readonly string SettingsFilePath;

        // Assets
        public readonly string AssetsPath;
        public readonly IntPtr SpritesheetTexture;

        // Engine stage
        public enum EngineStage { EnterName, Loading, Playing }
        public EngineStage ActiveStage { get; private set; }

        // Self state
        public struct EngineSelfState
        {
            public Guid Guid;
            public string PlayerName;
            public int PlayerIndex;
            public int BaseChunkX;
            public int BaseChunkY;
        }

        public EngineSelfState SelfState;

        // Player list
        public readonly List<PlayerListEntry> PlayerList = new List<PlayerListEntry>();

        // Map
        public readonly Map Map = new Map();
        public readonly byte[] FogOfWar = new byte[Map.MapSize * Map.MapSize];

        // Selected entity
        public Entity SelectedEntity { get; private set; }
        Entity.EntityDirection? _selectedEntityMoveDirection;

        // Scripting
        public string ScriptsPath { get; private set; }
        public readonly Dictionary<int, string> EntityScriptPaths = new Dictionary<int, string>();
        public readonly Dictionary<string, string> Scripts = new Dictionary<string, string>();

        readonly Dictionary<int, KeraLua.Lua> _luasByEntityId = new Dictionary<int, KeraLua.Lua>();

        // Ticking
        int _tickIndex;

        // Networking
        Socket _socket;
        PacketReceiver _receiver;
        readonly PacketWriter _writer = new PacketWriter();
        readonly PacketReader _reader = new PacketReader();

        // UI
        public readonly Desktop Desktop;
        public readonly EnterNameView EnterNameView;
        public readonly LoadingView LoadingView;
        public readonly InGameView InGameView;

        public Engine()
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_CreateWindowAndRenderer(1280, 720, 0, out Window, out Renderer);

            // Identity
            var identityPath = Path.Combine(AppContext.BaseDirectory, "Identity.dat");

            if (File.Exists(identityPath))
            {
                try { SelfState.Guid = new Guid(File.ReadAllBytes(identityPath)); } catch { }
            }

            if (SelfState.Guid == Guid.Empty)
            {
                SelfState.Guid = Guid.NewGuid();
                File.WriteAllBytes(identityPath, SelfState.Guid.ToByteArray());
            }

            // Settings
            SettingsFilePath = Path.Combine(AppContext.BaseDirectory, "Settings.txt");
            try { SelfState.PlayerName = File.ReadAllText(SettingsFilePath); } catch { }

            // Scripts
            ScriptsPath = Path.Combine(AppContext.BaseDirectory, "Scripts");
            if (!Directory.Exists(ScriptsPath)) Directory.CreateDirectory(ScriptsPath);

            foreach (var scriptFilePath in Directory.EnumerateFiles(ScriptsPath, "*.lua", SearchOption.AllDirectories))
            {
                var relativeFilePath = scriptFilePath.Substring(ScriptsPath.Length + 1);
                Scripts.Add(relativeFilePath, File.ReadAllText(scriptFilePath));
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
            _receiver = new PacketReceiver(_socket);

            Desktop.SetRootElement(EnterNameView);
            EnterNameView.NameInput.SetValue(SelfState.PlayerName ?? "");
            Desktop.FocusedElement = EnterNameView.NameInput;

            Run();
        }

        void Run()
        {
            var isRunning = true;
            var stopwatch = Stopwatch.StartNew();

            while (isRunning)
            {
                // Network
                if (_socket.Poll(0, SelectMode.SelectRead))
                {
                    if (!_receiver.Read(out var packets))
                    {
                        Trace.WriteLine($"Disconnected from server.");
                        // isRunning = false;
                        break;
                    }

                    void Abort(string reason)
                    {
                        _socket.Close();
                        isRunning = false;
                        Trace.WriteLine($"Abort: {reason}");
                    }

                    foreach (var packet in packets)
                    {
                        _reader.Open(packet);
                        if (!isRunning) break;

                        try
                        {
                            var packetType = (Protocol.ServerPacketType)_reader.ReadByte();

                            bool EnsureStage(EngineStage stage)
                            {
                                if (ActiveStage == stage) return true;
                                Abort($"Received packet {packetType} during wrong stage (expected {stage} but in {ActiveStage}.");
                                return false;
                            }

                            bool EnsureLoadingOrPlayingStage()
                            {
                                if (ActiveStage == EngineStage.Loading || ActiveStage == EngineStage.Playing) return true;
                                Abort($"Received packet {packetType} during wrong stage (expected Loading or Playing but in {ActiveStage}.");
                                return false;
                            }

                            switch (packetType)
                            {
                                case ServerPacketType.SetupPlayerIndex:
                                    if (!EnsureStage(EngineStage.Loading)) break;
                                    SelfState.PlayerIndex = _reader.ReadInt();
                                    break;

                                case ServerPacketType.PlayerList:
                                    if (!EnsureLoadingOrPlayingStage()) break;
                                    ReadPlayerList();
                                    break;

                                case ServerPacketType.Chat:
                                    if (!EnsureLoadingOrPlayingStage()) break;
                                    ReadChat();
                                    break;

                                case ServerPacketType.Tick:
                                    if (!EnsureLoadingOrPlayingStage()) break;
                                    if (SelfState.PlayerIndex == -1) { Abort("Received tick before receiving self player index."); break; }

                                    ReadTick();

                                    if (ActiveStage == EngineStage.Loading)
                                    {
                                        Desktop.SetRootElement(InGameView);
                                        Desktop.FocusedElement = InGameView;
                                        ActiveStage = EngineStage.Playing;
                                    }

                                    break;
                            }
                        }
                        catch (PacketException packetException)
                        {
                            Abort(packetException.Message);
                        }
                    }
                }

                // Input
                while (isRunning && SDL.SDL_PollEvent(out var @event) != 0)
                {
                    switch (@event.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            isRunning = false;
                            break;

                        case SDL.SDL_EventType.SDL_WINDOWEVENT:
                            switch (@event.window.windowEvent)
                            {
                                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                                    isRunning = false;
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

                if (!isRunning) break;

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
    }
}