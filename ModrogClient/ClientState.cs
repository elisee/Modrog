﻿using ModrogApi;
using ModrogCommon;
using SDL2;
using SwarmBasics.Math;
using SwarmCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ModrogClient
{
    enum ClientStage { Home, Loading, Lobby, Playing, Disconnected, Exited }

    partial class ClientState
    {
        public ClientStage Stage;

        // Networking
        Socket _socket;
        PacketReceiver _packetReceiver;
        readonly PacketWriter _packetWriter = new PacketWriter(capacity: 8192, useSizeHeader: true);
        readonly PacketReader _packetReader = new PacketReader();
        Process _serverProcess;

        public string SavedServerHostname = "localhost"; // TODO: Save and load from settings
        public int SavedServerPort = Protocol.Port;

        public string ErrorMessage { get; private set; }
        public string KickReason { get; private set; }

        // Self
        public Guid SelfGuid;
        public string SelfPlayerName;

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

        public readonly List<Game.ClientTileKind> TileKinds = new List<Game.ClientTileKind>();

        public Point WorldSize;
        public short[] WorldTiles;
        public byte[] WorldFog;
        public readonly List<Game.ClientEntity> SeenEntities = new List<Game.ClientEntity>();

        public Game.ClientEntity SelectedEntity;
        public EntityDirection? SelectedEntityMoveDirection;

        // Scripts
        public readonly Dictionary<int, string> EntityScriptPaths = new Dictionary<int, string>();
        public readonly Dictionary<string, string> Scripts = new Dictionary<string, string>();

        // Scripting

        // Ticking
        public int TickIndex;
        readonly Engine _engine;

        public ClientState(Engine engine) { _engine = engine; }

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
            _engine.Interface.OnStageChanged();

            ThreadPool.QueueUserWorkItem((_) =>
            {
                _socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true, LingerState = new LingerOption(true, seconds: 1) };

                IPAddress[] hostAddresses;
                try { hostAddresses = Dns.GetHostAddresses(hostname); }
                catch (Exception exception)
                {
                    _engine.RunOnEngineThread(() =>
                    {
                        ErrorMessage = $"Could not resolve hostname: {exception.Message}";
                        Stage = ClientStage.Home;
                        _engine.Interface.OnStageChanged();
                    });
                    return;
                }

                if (hostAddresses.Length == 0)
                {
                    _engine.RunOnEngineThread(() =>
                    {
                        ErrorMessage = $"Could not resolve hostname.";
                        Stage = ClientStage.Home;
                        _engine.Interface.OnStageChanged();
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

                        _engine.RunOnEngineThread(() =>
                        {
                            if (Stage != ClientStage.Loading) return;

                            if (connectionErrors.Count > 1) ErrorMessage = $"Could not connect after trying {connectionErrors.Count} addresses.\n{string.Join("\n", connectionErrors)}";
                            else ErrorMessage = $"Could not connect: {connectionErrors[0]}";
                            Stage = ClientStage.Home;
                            _engine.Interface.OnStageChanged();
                        });
                        return;
                    }
                }

                _engine.RunOnEngineThread(() =>
                {
                    LoadingProgressText = "Loading...";
                    _engine.Interface.LoadingView.OnProgress();

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
            var serverExePath = Path.Combine(FileHelper.FindAppFolder("ModrogServer-Debug"), "netcoreapp3.0", "ModrogServer.exe");
            _serverProcess = Process.Start(new ProcessStartInfo(serverExePath));

            // TODO: Replace this with a proper built-in server console
            ThreadPool.QueueUserWorkItem((_) =>
            {
                Task.Delay(100).Wait();
                _engine.RunOnEngineThread(() => SDL.SDL_RaiseWindow(_engine.Window));
            });

            Connect("127.0.0.1", Protocol.Port, scenario);
        }

        public void SetPlayingMenuOpen(bool isOpen)
        {
            PlayingMenuOpen = isOpen;
            _engine.Interface.PlayingView.OnMenuStateUpdated();
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
            _engine.Interface.OnStageChanged();
        }

        void SendPacket()
        {
            try { _socket.Send(_packetWriter.Buffer, 0, _packetWriter.Finish(), SocketFlags.None); } catch { }
        }

        #region Connect Stage
        public void SetName(string name)
        {
            SelfPlayerName = name;
            File.WriteAllText(_engine.SettingsFilePath, SelfPlayerName);
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
            _engine.Interface.PlayingView.OnSelectedEntityChanged();
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