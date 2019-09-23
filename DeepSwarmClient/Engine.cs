using DeepSwarmClient.UI;
using DeepSwarmCommon;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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
        public readonly string SettingsFilePath;

        public enum EngineStage { EnterName, Loading, Playing }
        public EngineStage ActiveStage { get; private set; }

        public readonly Guid SelfGuid;
        public string SelfPlayerName;
        public readonly List<PlayerListEntry> PlayerList = new List<PlayerListEntry>();
        public int SelfPlayerIndex = -1;
        public int SelfBaseChunkX { get; private set; }
        public int SelfBaseChunkY { get; private set; }

        public float ScrollingPixelsX { get; private set; }
        public float ScrollingPixelsY { get; private set; }

        public int HoveredTileX { get; private set; }
        public int HoveredTileY { get; private set; }
        public Entity SelectedEntity { get; private set; }
        public readonly Dictionary<int, string> EntityScripts = new Dictionary<int, string>();

        int _tickIndex;

        public readonly byte[] FogOfWar = new byte[Map.MapSize * Map.MapSize];

        public bool IsScrollingLeft;
        public bool IsScrollingRight;
        public bool IsScrollingUp;
        public bool IsScrollingDown;

        public Map Map = new Map();

        public readonly string ScriptsPath;
        public readonly Dictionary<string, string> Scripts = new Dictionary<string, string>();

        Socket _socket;
        PacketReceiver _receiver;
        readonly PacketWriter _writer = new PacketWriter();
        readonly PacketReader _reader = new PacketReader();

        public readonly Desktop Desktop;
        public readonly EnterNameView EnterNameView;
        public readonly LoadingView LoadingView;
        public readonly InGameView InGameView;

        public readonly IntPtr SpritesheetTexture;

        public Engine()
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_CreateWindowAndRenderer(1280, 720, 0, out Window, out Renderer);

            var identityPath = Path.Combine(AppContext.BaseDirectory, "Identity.dat");

            if (File.Exists(identityPath))
            {
                try { SelfGuid = new Guid(File.ReadAllBytes(identityPath)); } catch { }
            }

            if (SelfGuid == Guid.Empty)
            {
                SelfGuid = Guid.NewGuid();
                File.WriteAllBytes(identityPath, SelfGuid.ToByteArray());
            }

            SettingsFilePath = Path.Combine(AppContext.BaseDirectory, "Settings.txt");
            if (File.Exists(SettingsFilePath))
            {
                try { SelfPlayerName = File.ReadAllText(SettingsFilePath); } catch { }
            }

            ScriptsPath = Path.Combine(AppContext.BaseDirectory, "Scripts");
            foreach (var scriptFilePath in Directory.EnumerateFiles(ScriptsPath, "*.lua", SearchOption.AllDirectories))
            {
                var relativeFilePath = scriptFilePath.Substring(ScriptsPath.Length + 1);
                Scripts.Add(relativeFilePath, File.ReadAllText(scriptFilePath));
            }

            AssetsPath = FileHelper.FindAppFolder("Assets");

            if (SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG) != (int)SDL_image.IMG_InitFlags.IMG_INIT_PNG) throw new Exception();

            RendererHelper.FontTexture = SDL_image.IMG_LoadTexture(Renderer, Path.Combine(AssetsPath, "Font.png"));
            SpritesheetTexture = SDL_image.IMG_LoadTexture(Renderer, Path.Combine(AssetsPath, "Spritesheet.png"));

            Desktop = new Desktop(Renderer);

            EnterNameView = new EnterNameView(this);
            LoadingView = new LoadingView(this);
            InGameView = new InGameView(this);
        }

        public void CreateScriptForSelectedEntity()
        {
            string relativePath;
            var index = 0;
            var suffix = "";

            while (true)
            {
                relativePath = $"Script{suffix}.lua";
                if (!File.Exists(Path.Combine(ScriptsPath, relativePath))) break;
                index++;
                suffix = $"_{index}";
            }

            var defaultScriptText = "function tick(self)\n  \nend\n";
            File.WriteAllText(Path.Combine(ScriptsPath, relativePath), defaultScriptText);
            Scripts.Add(relativePath, defaultScriptText);
            InGameView.OnScriptListUpdated();

            SetupScriptForSelectedEntity(relativePath);
        }

        public void Start()
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                LingerState = new LingerOption(true, seconds: 1)
            };

            _socket.Connect(new IPEndPoint(IPAddress.Loopback, Protocol.Port));

            _receiver = new PacketReceiver(_socket);

            Desktop.SetRootElement(EnterNameView);
            EnterNameView.NameInput.Value = SelfPlayerName ?? "";
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

            HoveredTileX = ((int)ScrollingPixelsX + Desktop.MouseX) / Map.TileSize;
            HoveredTileY = ((int)ScrollingPixelsY + Desktop.MouseY) / Map.TileSize;
        }

        void Send()
        {
            try { _socket.Send(_writer.Buffer, 0, _writer.Finish(), SocketFlags.None); } catch { }
        }

        public void SetName(string name)
        {
            SelfPlayerName = name;
            File.WriteAllText(SettingsFilePath, SelfPlayerName);

            ActiveStage = EngineStage.Loading;
            Desktop.SetRootElement(LoadingView);
            Desktop.FocusedElement = null;

            _writer.WriteByteLengthString(Protocol.VersionString);
            _writer.WriteBytes(SelfGuid.ToByteArray());
            _writer.WriteByteLengthString(SelfPlayerName);
            Send();
        }

        public void SetSelectedEntity(Entity entity)
        {
            SelectedEntity = entity;
            InGameView.OnSelectedEntityChanged();
        }

        public void PlanMove(Entity.EntityMove move)
        {
            _writer.WriteByte((byte)Protocol.ClientPacketType.PlanMoves);
            _writer.WriteInt(_tickIndex);
            _writer.WriteShort(1);
            _writer.WriteInt(SelectedEntity.Id);
            _writer.WriteByte((byte)move);
            Send();
        }

        public void SetupScriptForSelectedEntity(string scriptFilePath)
        {
            EntityScripts[SelectedEntity.Id] = scriptFilePath;
            InGameView.OnSelectedEntityChanged();
        }

        public void ClearScriptForSelectedEntity()
        {
            EntityScripts.Remove(SelectedEntity.Id);
            InGameView.OnSelectedEntityChanged();
        }

        void ReadPlayerList()
        {
            PlayerList.Clear();

            var playerCount = _reader.ReadInt();

            for (var i = 0; i < playerCount; i++)
            {
                var name = _reader.ReadByteSizeString();
                var team = (PlayerTeam)_reader.ReadByte();
                var isOnline = _reader.ReadByte() != 0;

                PlayerList.Add(new PlayerListEntry { Name = name, Team = team, IsOnline = isOnline });
            }

            InGameView.OnPlayerListUpdated();
        }

        void ReadChat()
        {
            // TODO
        }

        void ReadTick()
        {
            Unsafe.InitBlock(ref FogOfWar[0], 0, (uint)FogOfWar.Length);
            Map.Entities.Clear();

            _tickIndex = _reader.ReadInt();

            Entity newSelectedEntity = null;

            var seenEntitiesCount = _reader.ReadShort();
            for (var i = 0; i < seenEntitiesCount; i++)
            {
                var entity = new Entity
                {
                    Id = _reader.ReadInt(),
                    X = _reader.ReadShort(),
                    Y = _reader.ReadShort(),
                    PlayerIndex = _reader.ReadShort(),
                    Type = (Entity.EntityType)_reader.ReadByte(),
                    Direction = (Entity.EntityDirection)_reader.ReadByte(),
                    Health = _reader.ReadByte(),
                };

                if (SelectedEntity?.Id == entity.Id) newSelectedEntity = entity;

                Map.Entities.Add(entity);

                if (entity.PlayerIndex == SelfPlayerIndex && entity.Type == Entity.EntityType.Factory)
                {
                    SelfBaseChunkX = entity.X / Map.ChunkSize;
                    SelfBaseChunkY = entity.Y / Map.ChunkSize;
                }
            }

            SelectedEntity = newSelectedEntity;

            var seenTilesCount = _reader.ReadShort();
            for (var i = 0; i < seenTilesCount; i++)
            {
                var x = _reader.ReadShort();
                var y = _reader.ReadShort();
                FogOfWar[y * Map.MapSize + x] = 1;
                Map.Tiles[y * Map.MapSize + x] = _reader.ReadByte();
            }

            // TODO: Scripting
            /* var validPlannedMoves = new Dictionary<int, Entity.EntityMove>();

            _writer.WriteByte((byte)Protocol.ClientPacketType.PlanMoves);
            _writer.WriteInt(_tickIndex);
            _writer.WriteShort((short)validPlannedMoves.Count);
            foreach (var (entityId, move) in validPlannedMoves)
            {
                _writer.WriteInt(entityId);
                _writer.WriteByte((byte)move);
            }
            Send();*/
        }
    }
}