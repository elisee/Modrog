using DeepSwarmCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DeepSwarmClient
{
    enum EngineView { Connect, Loading, Lobby, Playing, Disconnected }

    partial class ClientState
    {
        public bool IsRunning = true;

        // View
        public EngineView View;

        // Networking
        Socket _socket;
        PacketReceiver _packetReceiver;
        readonly PacketWriter _packetWriter = new PacketWriter();
        readonly PacketReader _packetReader = new PacketReader();

        public string SavedServerAddress = "localhost"; // TODO: Save and load from settings

        public string ErrorMessage { get; private set; }

        // Self
        public Guid SelfGuid;
        public string SelfPlayerName;

        // Loading
        public string LoadingProgressText { get; private set; }

        // Player list
        public readonly List<PlayerListEntry> PlayerList = new List<PlayerListEntry>();

        // Lobby
        bool _isSelfReady;

        public readonly List<ScenarioEntry> ScenarioEntries = new List<ScenarioEntry>();
        // public readonly List<SavedGameEntry> SavedGameEntries = new List<SavedGameEntry>();
        public ScenarioEntry ActiveScenario;

        // Playing

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

            IsRunning = false;
        }

        public void Connect(string address)
        {
            _isSelfReady = false;

            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true, LingerState = new LingerOption(true, seconds: 1) };

            LoadingProgressText = "Connecting...";
            View = EngineView.Loading;
            _engine.Interface.OnViewChanged();

            Task.Run(() =>
            {
                try
                {
                    _socket.Connect(new IPEndPoint(IPAddress.Loopback, Protocol.Port));
                }
                catch (Exception exception)
                {
                    _engine.RunOnEngineThread(() =>
                    {
                        if (exception is SocketException socketException) ErrorMessage = $"Could not connect: {socketException.SocketErrorCode}.";
                        else ErrorMessage = $"Could not connect: {exception.Message}";

                        View = EngineView.Connect;
                        _engine.Interface.OnViewChanged();
                    });

                    return;
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

        public void Disconnect(string error = null)
        {
            ErrorMessage = error;

            _socket?.Close();
            _socket = null;

            View = EngineView.Connect;
            _engine.Interface.OnViewChanged();
        }

        void SendPacket()
        {
            try { _socket.Send(_packetWriter.Buffer, 0, _packetWriter.Finish(), SocketFlags.None); } catch { }
        }

        #region Connect View
        public void SetName(string name)
        {
            SelfPlayerName = name;
            File.WriteAllText(_engine.SettingsFilePath, SelfPlayerName);
        }
        #endregion

        #region Lobby / Playing View
        public void SendChatMessage(string text)
        {
            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.Chat);
            _packetWriter.WriteByteSizeString(text);
            SendPacket();
        }
        #endregion

        #region Lobby View
        public void ChooseScenario(string scenarioName)
        {
            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.ChooseGame);
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

        #region Playing View
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
