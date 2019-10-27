using ModrogApi;
using ModrogCommon;
using SwarmBasics.Math;
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

        public string SavedServerHostname = "localhost"; // TODO: Save and load from settings
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

        public readonly Game.ClientTileKind[][] TileKindsByLayer = new Game.ClientTileKind[(int)MapLayer.Count][];

        public Dictionary<Point, Chunk> WorldChunks = new Dictionary<Point, Chunk>();
        public Dictionary<Point, Chunk> FogChunks = new Dictionary<Point, Chunk>();
        public readonly List<Game.ClientEntity> SeenEntities = new List<Game.ClientEntity>();

        public Game.ClientEntity SelectedEntity;
        public EntityDirection? SelectedEntityMoveDirection;

        // Scripts
        public readonly Dictionary<int, string> EntityScriptPaths = new Dictionary<int, string>();
        public readonly Dictionary<string, string> Scripts = new Dictionary<string, string>();

        // Scripting

        // Ticking
        public int TickIndex;
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
            // TODO: Add a byte to say whether scenario or saved game
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
            SelectedEntityMoveDirection = null;
            _app.PlayingView.OnSelectedEntityChanged();
        }

        public void SetMoveTowards(EntityDirection direction)
        {
            PlanMove(SelectedEntity.GetMoveForTargetDirection(direction));
            SelectedEntityMoveDirection = direction;
        }

        public void StopMovingTowards(EntityDirection direction)
        {
            if (SelectedEntityMoveDirection == direction) SelectedEntityMoveDirection = null;
        }

        public void PlanMove(ModrogApi.EntityMove move)
        {
            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.PlanMoves);
            _packetWriter.WriteInt(TickIndex);
            _packetWriter.WriteShort(1);
            _packetWriter.WriteInt(SelectedEntity.Id);
            _packetWriter.WriteByte((byte)move);
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
        }
    }
}
