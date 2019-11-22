using ModrogCommon;
using SwarmBasics.Math;
using SwarmBasics.Packets;
using SwarmCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ModrogClient
{
    enum ClientStage { Home, Loading, Lobby, Playing, Disconnected, Exited }

    partial class ClientState
    {
        public ClientStage Stage;

        // Networking
        Socket _socket;
        PacketReceiver _packetReceiver;
        readonly PacketWriter _packetWriter = new PacketWriter(initialCapacity: 8192, useSizeHeader: true);
        readonly PacketReader _packetReader = new PacketReader();
        Process _serverProcess;

        public string SavedServerHostname = "localhost";
        public int SavedServerPort = Protocol.Port;

        public string ErrorMessage { get; private set; }
        public string KickReason { get; private set; }

        // Identity
        public Guid SelfGuid;
        public string SelfPlayerName;
        public int SelfPlayerIndex;

        // Settings
        public readonly string SettingsFilePath;

        // Loading
        public string LoadingProgressText { get; private set; }

        // Player list
        public readonly List<PeerIdentity> PlayerList = new List<PeerIdentity>();

        // Lobby
        bool _isSelfReady;
        string _preselectedScenario;

        public readonly List<ScenarioEntry> ScenarioEntries = new List<ScenarioEntry>();
        // public readonly List<SavedGameEntry> SavedGameEntries = new List<SavedGameEntry>();
        public ScenarioEntry ActiveScenario;
        public bool IsCountingDown;

        // Playing
        public bool PlayingMenuOpen { get; private set; }

        public int TileSize { get; private set; }
        public readonly Game.ClientTileKind[][] TileKindsByLayer = new Game.ClientTileKind[(int)ModrogApi.MapLayer.Count][];

        public readonly List<Game.ClientCharacterKind> CharacterKinds = new List<Game.ClientCharacterKind>();
        public readonly List<Game.ClientItemKind> ItemKinds = new List<Game.ClientItemKind>();

        public Dictionary<Point, Chunk> WorldChunks = new Dictionary<Point, Chunk>();
        public Dictionary<Point, Chunk> FogChunks = new Dictionary<Point, Chunk>();
        public readonly List<Game.ClientEntity> EntitiesInSight = new List<Game.ClientEntity>();
        public readonly Dictionary<int, Game.ClientEntity> EntitiesInSightById = new Dictionary<int, Game.ClientEntity>();

        public Game.ClientEntity SelectedEntity;
        public ModrogApi.Direction? SelectedEntityMoveIntentDirection { get; private set; }

        // Ticking
        public int TickIndex { get; private set; }
        public float TickElapsedTime { get; private set; }
        public float TickProgress => Easing.InOutCubic(Math.Min(1f, TickElapsedTime / Protocol.TickInterval));
        readonly ClientApp _app;

        public ClientState(ClientApp app)
        {
            _app = app;

            // Identity
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

            // Settings
            SettingsFilePath = Path.Combine(AppContext.BaseDirectory, "Settings.txt");
            try { SelfPlayerName = File.ReadAllText(SettingsFilePath); } catch { }
        }

        public void Stop()
        {
            _socket?.Close();
            _socket = null;

            _serverProcess?.CloseMainWindow();
            _serverProcess = null;

            Stage = ClientStage.Exited;
        }

        public void Connect(string hostname, int port, string preselectedScenario = null)
        {
            SavedServerHostname = hostname;
            SavedServerPort = port;
            _preselectedScenario = preselectedScenario;

            _isSelfReady = false;
            ErrorMessage = null;
            KickReason = null;

            LoadingProgressText = "Connecting...";
            Stage = ClientStage.Loading;
            _app.OnStageChanged();

            ThreadPool.QueueUserWorkItem((_) =>
            {
                _socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true, LingerState = new LingerOption(true, seconds: 1) };

                IPAddress[] hostAddresses;
                try { hostAddresses = Dns.GetHostAddresses(hostname); }
                catch (Exception exception)
                {
                    _app.RunOnAppThread(() =>
                    {
                        ErrorMessage = $"Could not resolve hostname: {exception.Message}";
                        Stage = ClientStage.Home;
                        _app.OnStageChanged();
                    });
                    return;
                }

                if (hostAddresses.Length == 0)
                {
                    _app.RunOnAppThread(() =>
                    {
                        ErrorMessage = $"Could not resolve hostname.";
                        Stage = ClientStage.Home;
                        _app.OnStageChanged();
                    });
                    return;
                }

                var connectionErrors = new List<string>();

                for (var i = 0; i < hostAddresses.Length; i++)
                {
                    var hostAddress = hostAddresses[i];

                    try
                    {
                        _socket.Connect(new IPEndPoint(hostAddress, port));
                    }
                    catch (Exception exception)
                    {
                        connectionErrors.Add(exception.Message);

                        if (i < hostAddresses.Length - 1) continue;

                        _app.RunOnAppThread(() =>
                        {
                            if (Stage != ClientStage.Loading) return;

                            if (connectionErrors.Count > 1) ErrorMessage = $"Could not connect after trying {connectionErrors.Count} addresses.\n{string.Join("\n", connectionErrors)}";
                            else ErrorMessage = $"Could not connect: {connectionErrors[0]}";
                            Stage = ClientStage.Home;
                            _app.OnStageChanged();
                        });
                        return;
                    }
                }

                _app.RunOnAppThread(() =>
                {
                    LoadingProgressText = "Loading...";
                    _app.LoadingView.OnProgress();

                    _packetReceiver = new PacketReceiver(_socket);

                    _packetWriter.WriteByte((byte)Protocol.ClientPacketType.Hello);
                    _packetWriter.WriteByteSizeString(Protocol.VersionString);
                    _packetWriter.WriteBytes(SelfGuid.ToByteArray());
                    _packetWriter.WriteByteSizeString(SelfPlayerName);
                    SendPacket();
                });
            });
        }

        public void StartServer(string scenario)
        {
            var serverExePath = Path.Combine(FileHelper.FindAppFolder(
#if DEBUG
                "ModrogServer-Debug"
#else
                "ModrogServer-Release"
#endif
                ), "netcoreapp3.0", "ModrogServer.exe");
            _serverProcess = Process.Start(new ProcessStartInfo(serverExePath));

            Connect("127.0.0.1", Protocol.Port, scenario);
        }

        public void SetPlayingMenuOpen(bool isOpen)
        {
            PlayingMenuOpen = isOpen;
            _app.PlayingView.OnMenuStateUpdated();
        }

        public void Disconnect(string error = null)
        {
            ErrorMessage = error;

            WorldChunks.Clear();
            FogChunks.Clear();

            CharacterKinds.Clear();
            ItemKinds.Clear();

            SelectedEntity = null;
            SelectedEntityMoveIntentDirection = null;

            _socket?.Close();
            _socket = null;

            _serverProcess?.CloseMainWindow();
            _serverProcess = null;

            if (PlayingMenuOpen) SetPlayingMenuOpen(false);

            Stage = ClientStage.Home;
            _app.OnStageChanged();
        }

        void SendPacket()
        {
            try { _socket.Send(_packetWriter.Buffer, 0, _packetWriter.Finish(), SocketFlags.None); } catch { }
        }

        #region Connect Stage
        public void SetName(string name)
        {
            SelfPlayerName = name;
            File.WriteAllText(SettingsFilePath, SelfPlayerName);
        }
        #endregion

        #region Lobby / Playing Stages
        public void SendChatMessage(string text)
        {
            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.Chat);
            _packetWriter.WriteByteSizeString(text);
            SendPacket();
        }

        public void StopGame()
        {
            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.StopGame);
            SendPacket();
        }
        #endregion

        #region Lobby Stage
        public void SetScenario(string scenarioName)
        {
            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.SetScenario);
            _packetWriter.WriteByteSizeString(scenarioName);
            SendPacket();
        }

        public void ToggleReady()
        {
            _isSelfReady = !_isSelfReady;

            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.Ready);
            _packetWriter.WriteByte((byte)(_isSelfReady ? 1 : 0));
            SendPacket();
        }

        public void StartGame()
        {
            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.StartGame);
            SendPacket();
        }
        #endregion

        #region Playing Stage
        public void SelectEntity(Game.ClientEntity entity)
        {
            SelectedEntity = entity;
            SelectedEntityMoveIntentDirection = null;
        }

        public void SendSelfPlayerPosition(Point scrollPosition)
        {
            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.SetPlayerPosition);
            _packetWriter.WriteShortPoint(scrollPosition);
            SendPacket();
        }

        public void SetMoveIntent(ModrogApi.Direction direction)
        {
            SelectedEntityMoveIntentDirection = direction;

            WriteIntentHeader(ModrogApi.CharacterIntent.Move);
            _packetWriter.WriteByte((byte)direction);
            SendPacket();
        }

        public void SetUseIntent(ModrogApi.Direction direction, int slot)
        {
            SelectedEntityMoveIntentDirection = null;

            WriteIntentHeader(ModrogApi.CharacterIntent.Use);
            _packetWriter.WriteByte((byte)direction);
            _packetWriter.WriteByte((byte)slot);
            SendPacket();
        }

        public void SetSwapIntent(int slot, int itemEntityId)
        {
            SelectedEntityMoveIntentDirection = null;

            WriteIntentHeader(ModrogApi.CharacterIntent.Swap);
            _packetWriter.WriteByte((byte)slot);
            _packetWriter.WriteInt(itemEntityId);
            SendPacket();
        }

        void WriteIntentHeader(ModrogApi.CharacterIntent intent)
        {
            Debug.Assert(SelectedEntity.PlayerIndex == SelfPlayerIndex);

            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.SetEntityIntents);
            _packetWriter.WriteInt(TickIndex);
            _packetWriter.WriteShort(1);
            _packetWriter.WriteInt(SelectedEntity.Id);
            _packetWriter.WriteByte((byte)intent);
        }

        public void ClearMoveIntent(ModrogApi.Direction direction)
        {
            if (SelectedEntityMoveIntentDirection == direction) SelectedEntityMoveIntentDirection = null;
        }

        void SendIntents()
        {
            var intents = new Dictionary<int, (ModrogApi.CharacterIntent, ModrogApi.Direction, int)>();

            if (SelectedEntity != null && SelectedEntityMoveIntentDirection != null)
            {
                intents[SelectedEntity.Id] = (ModrogApi.CharacterIntent.Move, SelectedEntityMoveIntentDirection.Value, 0);
            }

            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.SetEntityIntents);
            _packetWriter.WriteInt(TickIndex);
            _packetWriter.WriteShort((short)intents.Count);
            foreach (var (entityId, (intent, direction, slot)) in intents)
            {
                _packetWriter.WriteInt(entityId);
                _packetWriter.WriteByte((byte)intent);
                if (intent == ModrogApi.CharacterIntent.Move || intent == ModrogApi.CharacterIntent.Use) _packetWriter.WriteByte((byte)direction);
                if (intent == ModrogApi.CharacterIntent.Use || intent == ModrogApi.CharacterIntent.Swap) _packetWriter.WriteByte((byte)slot);
            }
            SendPacket();
        }
        #endregion

        internal void Update(float deltaTime)
        {
            if (_socket != null && _socket.Poll(0, SelectMode.SelectRead))
            {
                if (!_packetReceiver.Read(out var packets))
                {
                    Disconnect(error: "Connection lost while trying to read from server.");
                    return;
                }

                ReadPackets(packets);
            }

            if (Stage == ClientStage.Playing)
            {
                var hasSentIntents = TickElapsedTime >= Protocol.TickInterval / 2f;
                TickElapsedTime += deltaTime;
                var shouldSendIntents = TickElapsedTime >= Protocol.TickInterval / 2f;

                if (!hasSentIntents && shouldSendIntents) SendIntents();
            }
        }
    }
}
