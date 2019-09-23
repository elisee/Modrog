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
        public int SelfPlayerIndex = -1;
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
                    int bytesRead = 0;
                    try { bytesRead = _socket.Receive(_reader.Buffer); } catch (SocketException) { }

                    if (bytesRead == 0)
                    {
                        Trace.WriteLine($"Disconnected from server.");
                        // isRunning = false;
                        break;
                    }

                    Trace.WriteLine($"Received {bytesRead} bytes.");

                    void Abort(string reason)
                    {
                        _socket.Close();
                        isRunning = false;
                        Trace.WriteLine($"Abort: {reason}");
                    }

                    _reader.ResetCursor();

                    while (isRunning && _reader.Cursor < bytesRead)
                    {
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
                                    SelfPlayerIndex = _reader.ReadInt();
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
                                    if (SelfPlayerIndex == -1) { Abort("Received tick before receiving self player index."); break; }

                                    ReadTick();

                                    if (ActiveStage == EngineStage.Loading)
                                    {
                                        ScrollingPixelsX = (int)((SelfBaseChunkX + 0.5f) * Map.ChunkSize * Map.TileSize) - Viewport.Width / 2;
                                        ScrollingPixelsY = (int)((SelfBaseChunkY + 0.5f) * Map.ChunkSize * Map.TileSize) - Viewport.Height / 2;

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
                var angle = MathF.Atan2(dy, dx);
                ScrollingPixelsX += MathF.Cos(angle) * ScrollingSpeed * deltaTime;
                ScrollingPixelsY -= MathF.Sin(angle) * ScrollingSpeed * deltaTime;
            }
        }

        void Send()
        {
            try { _socket.Send(_writer.Buffer, 0, _writer.Cursor, SocketFlags.None); } catch { }
        }

        public void SetName(string name)
        {
            ActiveStage = EngineStage.Loading;
            Desktop.SetRootElement(LoadingView);
            Desktop.FocusedElement = null;

            _writer.ResetCursor();
            _writer.WriteByteLengthString(Protocol.VersionString);
            _writer.WriteBytes(SelfGuid.ToByteArray());
            _writer.WriteByteLengthString(name);
            Send();
        }

        void ReadPlayerList()
        {
            PlayerList.Clear();

            var playerCount = _reader.ReadInt();

            for (var i = 0; i < playerCount; i++)
            {
                var name = _reader.ReadByteSizeString();
                var team = (PlayerTeam)_reader.ReadByte();
                var isOnline = _reader.ReadByte() == 0;

                PlayerList.Add(new PlayerListEntry { Name = name, Team = team, IsOnline = isOnline });
            }

            InGameView.OnPlayerListUpdated();
        }

        void ReadChat()
        {
            // TODO
        }

        /* void ReadMapArea()
        {
            var x = _reader.ReadShort();
            var y = _reader.ReadShort();
            var width = _reader.ReadShort();
            var height = _reader.ReadShort();

            var area = _reader.ReadBytes(width * height).ToArray();
            for (var j = 0; j < height; j++) Buffer.BlockCopy(area, j * width, Map.Tiles, (y + j) * Map.MapSize + x, width);
        } */

        void ReadTick()
        {
            Map.Entities.Clear();

            // TODO: Handle fog of war with an additional array holding whether each tile is currently being seen or not

            var seenEntitiesCount = _reader.ReadShort();
            for (var i = 0; i < seenEntitiesCount; i++)
            {
                var x = (int)_reader.ReadShort();
                var y = (int)_reader.ReadShort();
                var playerIndex = _reader.ReadShort();
                var type = (Entity.EntityType)_reader.ReadByte();
                var direction = (Entity.EntityDirection)_reader.ReadByte();
                var health = (int)_reader.ReadByte();
                var entity = new Entity(type, playerIndex, x, y, direction) { Health = health };
                Map.Entities.Add(entity);

                if (playerIndex == SelfPlayerIndex && type == Entity.EntityType.Factory)
                {
                    SelfBaseChunkX = x / Map.ChunkSize;
                    SelfBaseChunkY = y / Map.ChunkSize;
                }
            }

            var seenTilesCount = _reader.ReadShort();
            for (var i = 0; i < seenTilesCount; i++)
            {
                var x = _reader.ReadShort();
                var y = _reader.ReadShort();
                Map.Tiles[y * Map.MapSize + x] = _reader.ReadByte();
            }
        }
    }
}