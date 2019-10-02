using DeepSwarmCommon;
using System;
using System.Linq;

namespace DeepSwarmServer
{
    partial class ServerState
    {
        void ReadPacket(Peer peer)
        {
            var packetType = (Protocol.ClientPacketType)_packetReader.ReadByte();

            if (peer.Identity == null)
            {
                if (packetType != Protocol.ClientPacketType.Hello) throw new PacketException($"Invalid packet type, expected {nameof(Protocol.ClientPacketType.Hello)}, got {packetType}.");
                ReadHello(peer);
                return;
            }

            if (packetType == Protocol.ClientPacketType.Chat)
            {
                ReadChat(peer);
                var text = _packetReader.ReadByteSizeString();
                _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Chat);
                _packetWriter.WriteByteSizeString(peer.Identity.Name);
                _packetWriter.WriteByteSizeString(text);
                Broadcast();
                return;
            }

            switch (_stage)
            {
                case ServerStage.Lobby:
                    switch (packetType)
                    {
                        case Protocol.ClientPacketType.ChooseGame: ReadChooseGame(peer); break;
                        case Protocol.ClientPacketType.StartGame: ReadStartGame(peer); break;
                        case Protocol.ClientPacketType.Ready: ReadSetReady(peer); break;
                        default:
                            throw new PacketException($"Invalid packet type for {nameof(ServerStage.Lobby)} stage: {packetType}.");
                    }
                    break;

                case ServerStage.Playing:
                    switch (packetType)
                    {
                        case Protocol.ClientPacketType.PlanMoves: ReadPlanMoves(peer); break;
                    }

                    break;
            }
        }

        void ReadHello(Peer peer)
        {
            string versionString;
            try { versionString = _packetReader.ReadByteSizeString(); }
            catch { throw new PacketException("Invalid version string in {nameof(Protocol.ClientPacketType.Hello)} packet."); }
            if (versionString != Protocol.VersionString) throw new PacketException($"Invalid version string, expected {Protocol.VersionString}, got {versionString}.");

            Guid guid;
            try { guid = new Guid(_packetReader.ReadBytes(16)); if (guid == Guid.Empty) throw new Exception(); }
            catch { throw new PacketException($"Invalid player identity in {nameof(Protocol.ClientPacketType.Hello)} packet."); }

            string name;
            try { name = _packetReader.ReadByteSizeString(); }
            catch { throw new PacketException($"Invalid peer name in {nameof(Protocol.ClientPacketType.Hello)} packet."); }
            if (!Protocol.PlayerNameRegex.IsMatch(name)) throw new PacketException($"Invalid player name: {name}.");
            if (_playerIdentities.Any(x => x.Guid != guid && x.Name == name)) throw new PacketException($"Name already in use.");

            peer.Identity = _playerIdentities.Find(x => x.Guid == guid);

            if (peer.Identity != null)
            {
                if (peer.Identity.IsOnline) throw new PacketException($"There is already someone connected with that player identity.");
                peer.Identity.IsOnline = true;
            }
            else
            {
                peer.Identity = new PlayerIdentity { Guid = guid, IsOnline = true };
                _playerIdentities.Add(peer.Identity);
            }

            if (_hostGuid == Guid.Empty || _hostGuid == peer.Identity.Guid)
            {
                // This is the first player to successfully connect, make them host
                _hostGuid = peer.Identity.Guid;
                peer.Identity.IsHost = true;
            }

            peer.Identity.Name = name;
            _unindentifiedPeerSockets.Remove(peer.Socket);
            _identifiedPeerSockets.Add(peer.Socket);

            BroadcastPlayerList();

            // Reply
            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Welcome);
            _packetWriter.WriteByte((byte)_stage);

            switch (_stage)
            {
                case ServerStage.Lobby:
                    _packetWriter.WriteByte((byte)_scenarioEntries.Count);
                    foreach (var entry in _scenarioEntries)
                    {
                        _packetWriter.WriteByteSizeString(entry.Name);
                        _packetWriter.WriteByte((byte)entry.MinPlayers);
                        _packetWriter.WriteByte((byte)entry.MaxPlayers);
                        _packetWriter.WriteByte((byte)entry.SupportedModes);
                        _packetWriter.WriteShortSizeString(entry.Description);
                    }

                    /*
                    _packetWriter.WriteByte((byte)_savedGameEntries.Count);

                    foreach (var entry in _savedGameEntries)
                    {
                        throw new NotImplementedException();
                    }
                    */

                    _packetWriter.WriteByteSizeString(_activeScenario?.Name ?? "");
                    break;

                case ServerStage.Playing:
                    throw new NotImplementedException();
            }

            Send(peer.Socket);
        }

        #region Lobby and Playing Stages
        void ReadChat(Peer peer)
        {
            var message = _packetReader.ReadByteSizeString();

            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Chat);
            _packetWriter.WriteByteSizeString(peer.Identity.Name);
            _packetWriter.WriteByteSizeString(message);
            Broadcast();
        }
        #endregion

        #region Lobby Stage
        void ReadChooseGame(Peer peer)
        {
            if (!peer.Identity.IsHost) throw new PacketException("Can't choose game if not host.");

            var scenarioName = _packetReader.ReadByteSizeString();
            var scenario = _scenarioEntries.Find(x => x.Name == scenarioName);
            if (scenario == null) throw new PacketException($"Unknown scenario: {scenarioName}.");
            _activeScenario = scenario;

            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.SetupGame);
            _packetWriter.WriteByteSizeString(scenario.Name);
            Broadcast();
        }

        void ReadStartGame(Peer peer)
        {
            if (!peer.Identity.IsHost) throw new PacketException("Can't start game if not host.");

            if (_activeScenario == null || _playerIdentities.Any(x => !x.IsReady))
            {
                // Ignore
                return;
            }

            // TODO: Send a countdown instead
            _stage = ServerStage.Playing;
        }

        void ReadSetReady(Peer peer)
        {
            peer.Identity.IsReady = _packetReader.ReadByte() != 0;
            BroadcastPlayerList();
        }
        #endregion

        #region Playing Stage
        void ReadPlanMoves(Peer peer)
        {
            var clientTickIndex = _packetReader.ReadInt();
            if (clientTickIndex != _tickIndex)
            {
                Console.WriteLine($"{peer.Socket.RemoteEndPoint} - Ignoring {nameof(Protocol.ClientPacketType.PlanMoves)} packet from tick {clientTickIndex}, we're at {_tickIndex}.");
                return;
            }

            /*
            var moveCount = reader.ReadShort();
            for (var i = 0; i < moveCount; i++)
            {
                var entityId = reader.ReadInt();
                var move = (Entity.EntityMove)reader.ReadByte();
                if (!map.EntitiesById.TryGetValue(entityId, out var entity))
                {
                    throw new PacketException($"Invalid entity id in {nameof(Protocol.ClientPacketType.PlanMoves)} packet.");
                }

                if (entity.PlayerIndex != peer.Player.PlayerIndex)
                {
                    throw new PacketException($"Can't move entity not owned in {nameof(Protocol.ClientPacketType.PlanMoves)} packet.");
                }

                entity.UpcomingMove = move;
            }
            */
        }
        #endregion
    }
}
