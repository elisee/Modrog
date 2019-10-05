using DeepSwarmCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Net;
using System.Net.Sockets;

namespace DeepSwarmServer
{
    enum ServerStage { Lobby, Playing }

    partial class ServerState
    {
        ServerStage _stage = ServerStage.Lobby;
        Guid _hostGuid = Guid.Empty;
        readonly string _scenariosPath;

        // Networking
        Socket _listenerSocket;

        readonly Dictionary<Socket, Peer> _peersBySocket = new Dictionary<Socket, Peer>();
        readonly List<Socket> _unindentifiedPeerSockets = new List<Socket>();
        readonly List<Socket> _identifiedPeerSockets = new List<Socket>();
        readonly List<Game.PlayerIdentity> _playerIdentities = new List<Game.PlayerIdentity>();

        readonly List<Socket> _pollSockets = new List<Socket>();

        readonly PacketWriter _packetWriter = new PacketWriter();
        readonly PacketReader _packetReader = new PacketReader();

        // Lobby
        readonly List<ScenarioEntry> _scenarioEntries = new List<ScenarioEntry>();
        // readonly List<SavedGameEntry> _savedGameEntries = new List<SavedGameEntry>();

        // Playing
        ScenarioEntry _activeScenario;

        float _tickAccumulatedTime = 0f;
        int _tickIndex = -1;
        const float TickInterval = 0.2f;

        public ServerState(Guid hostGuid)
        {
            _hostGuid = hostGuid;

            _scenariosPath = FileHelper.FindAppFolder("Scenarios");

            foreach (var folder in Directory.GetDirectories(_scenariosPath, "*.*", SearchOption.TopDirectoryOnly))
            {
                var manifestJson = JsonValue.Parse(File.ReadAllText(Path.Combine(folder, "Manifest.json")));

                var entry = new ScenarioEntry
                {
                    Name = folder[(_scenariosPath.Length + 1)..],
                    Title = manifestJson["title"],
                    MinPlayers = manifestJson["minMaxPlayers"][0],
                    MaxPlayers = manifestJson["minMaxPlayers"][1],
                    SupportsCoop = manifestJson.ContainsKey("supportsCoop") && manifestJson["supportsCoop"],
                    SupportsVersus = manifestJson.ContainsKey("supportsVersus") && manifestJson["supportsVersus"],
                    Description = File.ReadAllText(Path.Combine(folder, "Description.txt")).Replace("\r", "")
                };

                _scenarioEntries.Add(entry);
            }
        }

        public void Start()
        {
            _listenerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                LingerState = new LingerOption(true, seconds: 1)
            };

            _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, Protocol.Port));
            _listenerSocket.Listen(64);
            Console.WriteLine($"Server listening on port {Protocol.Port}.");
        }

        public void Stop()
        {
            _listenerSocket.Close();
        }

        public void Update(float deltaTime)
        {
            _pollSockets.Clear();
            _pollSockets.Add(_listenerSocket);
            _pollSockets.AddRange(_unindentifiedPeerSockets);
            _pollSockets.AddRange(_identifiedPeerSockets);

            Socket.Select(_pollSockets, null, null, 0);

            foreach (var readSocket in _pollSockets)
            {
                if (readSocket == _listenerSocket)
                {
                    var newSocket = _listenerSocket.Accept();
                    _unindentifiedPeerSockets.Add(newSocket);
                    _peersBySocket.Add(newSocket, new Peer(newSocket));
                    Console.WriteLine($"{newSocket.RemoteEndPoint} - Socket connected, waiting for {nameof(Protocol.ClientPacketType.Hello)} packet.");
                }
                else
                {
                    ReadFromPeer(_peersBySocket[readSocket]);
                }
            }

            if (_stage == ServerStage.Playing)
            {
                _tickAccumulatedTime += deltaTime;

                if (_tickAccumulatedTime > TickInterval)
                {
                    Tick();
                    _tickAccumulatedTime %= TickInterval;
                }
            }
        }

        void ReadFromPeer(Peer peer)
        {
            if (!peer.Receiver.Read(out var packets)) { KickPeer(peer, null); return; }

            foreach (var packet in packets)
            {
                try
                {
                    _packetReader.Open(packet);
                    ReadPacket(peer);
                }
                catch (PacketException exception)
                {
                    KickPeer(peer, exception.Message);
                }
                catch (Exception exception)
                {
                    KickPeer(peer, $"Unhandled exception: {exception.Message}");
                }
            }
        }

        void KickPeer(Peer peer, string kickReason)
        {
            if (kickReason != null)
            {
                _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Kick);
                _packetWriter.WriteByteSizeString(kickReason);
                Send(peer.Socket);
            }

            var endPoint = peer.Socket.RemoteEndPoint;
            peer.Socket.Close();

            _peersBySocket.Remove(peer.Socket);

            if (peer.Identity != null)
            {
                peer.Identity.IsOnline = false;
                _identifiedPeerSockets.Remove(peer.Socket);
                _playerIdentities.Remove(peer.Identity);
                BroadcastPlayerList();
            }
            else
            {
                _unindentifiedPeerSockets.Remove(peer.Socket);
            }

            if (kickReason == null) Console.WriteLine($"{endPoint} - Socket disconnected.");
            else Console.WriteLine($"{endPoint} - Kicked: {kickReason}");
        }

        void Broadcast()
        {
            var length = _packetWriter.Finish();
            // Console.WriteLine($"Broadcasting {length} bytes");

            foreach (var socket in _identifiedPeerSockets)
            {
                try { socket.Send(_packetWriter.Buffer, 0, length, SocketFlags.None); } catch { }
            }
        }

        void Send(Socket socket)
        {
            var length = _packetWriter.Finish();
            // Console.WriteLine($"Sending {length} bytes");

            try { socket.Send(_packetWriter.Buffer, 0, length, SocketFlags.None); } catch { }
        }

        void BroadcastPlayerList()
        {
            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.PlayerList);
            _packetWriter.WriteInt(_playerIdentities.Count);

            foreach (var identity in _playerIdentities)
            {
                _packetWriter.WriteByteSizeString(identity.Name);
                _packetWriter.WriteByte((byte)((identity.IsHost ? 1 : 0) | (identity.IsOnline ? 2 : 0) | (identity.IsReady ? 4 : 0)));
            }

            Broadcast();
        }

        void Tick()
        {
            throw new NotImplementedException();
        }
    }
}
