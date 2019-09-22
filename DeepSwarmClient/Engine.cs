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
using static DeepSwarmCommon.Player;
using static DeepSwarmCommon.Protocol;

namespace DeepSwarmClient
{
    class Engine
    {

        public readonly Rectangle Viewport = new Rectangle(0, 0, 1280, 720);
        public readonly IntPtr Window;
        public readonly IntPtr Renderer;

        public readonly string AssetsPath;

        public enum EngineStage { EnterName, Loading, Playing }
        public EngineStage ActiveStage { get; private set; }

        public readonly Guid SelfGuid;
        public readonly List<PlayerListEntry> PlayerList = new List<PlayerListEntry>();
        public int SelfBaseChunkX { get; private set; }
        public int SelfBaseChunkY { get; private set; }

        public float ScrollingPixelsX { get; private set; }
        public float ScrollingPixelsY { get; private set; }

        public bool IsScrollingLeft;
        public bool IsScrollingRight;
        public bool IsScrollingUp;
        public bool IsScrollingDown;

        public Map Map = new Map();

        Socket _socket;
        readonly PacketWriter _writer = new PacketWriter();
        readonly PacketReader _reader = new PacketReader();

        public readonly Desktop Desktop;
        public readonly EnterNameView EnterNameView;
        public readonly LoadingView LoadingView;
        public readonly InGameView InGameView;

        public Engine()
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_CreateWindowAndRenderer(1280, 720, 0, out Window, out Renderer);

            var identityPath = Path.Combine(AppContext.BaseDirectory, "Identity.dat");

            if (File.Exists(identityPath))
            {
                try { SelfGuid = new Guid(File.ReadAllBytes(identityPath)); }
                catch { }
            }

            if (SelfGuid == Guid.Empty)
            {
                SelfGuid = Guid.NewGuid();
                File.WriteAllBytes(identityPath, SelfGuid.ToByteArray());
            }

            AssetsPath = FileHelper.FindAppFolder("Assets");

            SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG);
            RendererHelper.FontTexture = SDL_image.IMG_LoadTexture(Renderer, Path.Combine(AssetsPath, "Font.png"));

            Desktop = new Desktop(Renderer);

            EnterNameView = new EnterNameView(this);
            LoadingView = new LoadingView(this);
            InGameView = new InGameView(this);
        }

        public void Start()
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                LingerState = new LingerOption(true, seconds: 1)
            };

            _socket.Connect(new IPEndPoint(IPAddress.Loopback, Protocol.Port));

            _writer.ResetCursor();
            _writer.WriteByteLengthString(Protocol.VersionString);
            _writer.WriteBytes(SelfGuid.ToByteArray());
            _socket.Send(_writer.Buffer, 0, _writer.Cursor, SocketFlags.None);

            Desktop.SetRootElement(EnterNameView);
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
                    int bytesRead;
                    try { bytesRead = _socket.Receive(_reader.Buffer); }
                    catch (SocketException) { OnDisconnectedFromServer(); break; }
                    if (bytesRead == 0) { OnDisconnectedFromServer(); break; }

                    Trace.WriteLine($"Received {bytesRead} bytes.");

                    void OnDisconnectedFromServer()
                    {
                        Trace.WriteLine($"Disconnected from server.");
                        isRunning = false;
                    }

                    void Abort(string reason)
                    {
                        _socket.Close();
                        isRunning = false;
                        Trace.WriteLine($"Abort: {reason}");
                    }

                    _reader.ResetCursor();

                    while (_reader.Cursor < bytesRead)
                    {
                        try
                        {
                            switch (ActiveStage)
                            {
                                case EngineStage.EnterName:
                                    Abort($"Received packet during {nameof(EngineStage.EnterName)} stage.");
                                    break;

                                case EngineStage.Loading:
                                    {
                                        var packetType = (Protocol.ServerPacketType)_reader.ReadByte();

                                        switch (packetType)
                                        {
                                            case ServerPacketType.PlayerList:
                                                ReadPlayerList();
                                                break;
                                            case ServerPacketType.Setup:
                                                SelfBaseChunkX = _reader.ReadShort();
                                                SelfBaseChunkY = _reader.ReadShort();
                                                ReadMapArea();

                                                ActiveStage = EngineStage.Playing;
                                                ScrollingPixelsX = (int)((SelfBaseChunkX + 0.5f) * Map.ChunkSize * Map.TileSize) - Viewport.Width / 2;
                                                ScrollingPixelsY = (int)((SelfBaseChunkY + 0.5f) * Map.ChunkSize * Map.TileSize) - Viewport.Height / 2;

                                                Desktop.SetRootElement(InGameView);
                                                Desktop.FocusedElement = InGameView;
                                                break;
                                            default:
                                                Abort($"Received unexpected packet type during {nameof(EngineStage.Loading)}: {packetType}");
                                                break;
                                        }
                                    }
                                    break;

                                case EngineStage.Playing:
                                    {
                                        var packetType = (Protocol.ServerPacketType)_reader.ReadByte();
                                        switch (packetType)
                                        {
                                            case ServerPacketType.PlayerList: ReadPlayerList(); break;
                                            case ServerPacketType.Tick: ReadTick(); break;
                                            default: Abort($"Received invalid packet type: {packetType}"); break;
                                        }
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

                if (ActiveStage == EngineStage.Playing) Update(deltaTime);

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
            const float ScrollingSpeed = 400;
            var dx = 0;
            var dy = 0;

            if (IsScrollingLeft) dx--;
            if (IsScrollingRight) dx++;
            if (IsScrollingDown) dy--;
            if (IsScrollingUp) dy++;

            if (dx != 0 || dy != 0)
            {
                var angle = Math.Atan2(dy, dx);
                ScrollingPixelsX += (float)Math.Cos(angle) * ScrollingSpeed * deltaTime;
                ScrollingPixelsY -= (float)Math.Sin(angle) * ScrollingSpeed * deltaTime;
            }
        }

        public void SetName(string name)
        {
            ActiveStage = EngineStage.Loading;
            Desktop.SetRootElement(LoadingView);
            Desktop.FocusedElement = null;

            _writer.ResetCursor();
            _writer.WriteByteLengthString(name);
            _socket.Send(_writer.Buffer, 0, _writer.Cursor, SocketFlags.None);
        }

        void ReadPlayerList()
        {
            PlayerList.Clear();

            var playerCount = _reader.ReadInt();

            for (var i = 0; i < playerCount; i++)
            {
                var name = _reader.ReadByteSizeString();
                var team = _reader.ReadByte() == 0 ? PlayerTeam.Blue : PlayerTeam.Red;

                PlayerList.Add(new PlayerListEntry { Name = name, Team = team });
            }

            InGameView.OnPlayerListUpdated();
        }

        void ReadMapArea()
        {
            var x = _reader.ReadShort();
            var y = _reader.ReadShort();
            var width = _reader.ReadShort();
            var height = _reader.ReadShort();

            var area = _reader.ReadBytes(width * height).ToArray();
            for (var j = 0; j < height; j++) Buffer.BlockCopy(area, j * width, Map.Tiles, (y + j) * Map.MapSize + x, width);
        }

        void ReadTick()
        {

        }
    }
}