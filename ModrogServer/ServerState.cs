using ModrogCommon;
using SwarmCore;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ModrogServer
{
    enum ServerStage { Lobby, CountingDown, Playing }

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
        readonly List<PeerIdentity> _peerIdentities = new List<PeerIdentity>();

        readonly List<Socket> _pollSockets = new List<Socket>();

        readonly PacketWriter _packetWriter = new PacketWriter(capacity: 8192, useSizeHeader: true);
        readonly PacketReader _packetReader = new PacketReader();

        // Lobby
        readonly List<ScenarioEntry> _scenarioEntries = new List<ScenarioEntry>();
        // readonly List<SavedGameEntry> _savedGameEntries = new List<SavedGameEntry>();

        // CountingDown
        float _startCountdownTimer;

        // Playing
        string _scenarioName;
        byte[] _spritesheetBytes;
        Game.InternalUniverse _universe;
        float _tickAccumulatedTime = 0f;

        public ServerState(Guid hostGuid)
        {
            _hostGuid = hostGuid;

            _scenariosPath = FileHelper.FindAppFolder("Scenarios");
            _scenarioEntries = ScenarioEntry.ReadScenarioEntries(_scenariosPath);
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

            switch (_stage)
            {
                case ServerStage.CountingDown:
                    var previousCountdown = (int)(Protocol.StartCountdownDuration - _startCountdownTimer);

                    _startCountdownTimer += deltaTime;

                    var newCountdown = (int)(Protocol.StartCountdownDuration - _startCountdownTimer);

                    if (previousCountdown != newCountdown)
                    {
                        _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Chat);
                        _packetWriter.WriteByteSizeString("");
                        _packetWriter.WriteByteSizeString($"Game starts in {previousCountdown}s...");
                        Broadcast();
                    }

                    if (_startCountdownTimer >= Protocol.StartCountdownDuration) StartPlaying();
                    break;
                case ServerStage.Playing:
                    _tickAccumulatedTime += deltaTime;

                    if (_tickAccumulatedTime > Protocol.TickInterval)
                    {
                        Tick();
                        _tickAccumulatedTime %= Protocol.TickInterval;
                    }
                    break;
            }
        }

        void ReadFromPeer(Peer peer)
        {
            if (!peer.Receiver.Read(out var packets)) { RemovePeer(peer, null); return; }

            foreach (var packet in packets)
            {
                try
                {
                    _packetReader.Open(packet);
                    ReadPacket(peer);
                }
                catch (PacketException exception)
                {
                    RemovePeer(peer, exception.Message);
                }
                catch (Exception exception)
                {
                    RemovePeer(peer, $"Unhandled exception: {exception.Message}");
                }
            }

            void RemovePeer(Peer peer, string kickReason)
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

                    if (_stage == ServerStage.Lobby)
                    {
                        _peerIdentities.Remove(peer.Identity);
                        BroadcastPeerList();
                    }
                    else
                    {
                        _packetWriter.WriteByte((byte)Protocol.ServerPacketType.SetPeerOnline);
                        _packetWriter.WriteByteSizeString(peer.Identity.Name);
                        _packetWriter.WriteByte(0);
                        Broadcast();
                    }
                }
                else
                {
                    _unindentifiedPeerSockets.Remove(peer.Socket);
                }

                if (kickReason == null) Console.WriteLine($"{endPoint} - Socket disconnected.");
                else Console.WriteLine($"{endPoint} - Kicked: {kickReason}");
            }
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

        void BroadcastPeerList()
        {
            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.PeerList);
            _packetWriter.WriteInt(_peerIdentities.Count);

            foreach (var identity in _peerIdentities)
            {
                _packetWriter.WriteByteSizeString(identity.Name);
                _packetWriter.WriteByte((byte)((identity.IsHost ? 1 : 0) | (identity.IsOnline ? 2 : 0) | (identity.IsReady ? 4 : 0)));
                _packetWriter.WriteByte((byte)identity.PlayerIndex);
            }

            Broadcast();
        }
    }
}
