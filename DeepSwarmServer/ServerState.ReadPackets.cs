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
                if (packetType != Protocol.ClientPacketType.Hello) { KickPeer(peer, $"Invalid packet type, expected {nameof(Protocol.ClientPacketType.Hello)}, got {packetType}."); return; }
                ReadHelloPacket(peer);
                return;
            }

            if (packetType == Protocol.ClientPacketType.Chat)
            {
                ReadChatPacket(peer);
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
                        case Protocol.ClientPacketType.SetupGame:
                            throw new NotImplementedException();
                            break;
                        case Protocol.ClientPacketType.StartGame:
                            throw new NotImplementedException();
                            break;
                        case Protocol.ClientPacketType.Ready:
                            throw new NotImplementedException();
                            break;
                        default:
                            throw new PacketException($"Invalid packet type for {nameof(ServerStage.Lobby)} stage: {packetType}.");
                    }
                    break;

                case ServerStage.Playing:
                    switch (packetType)
                    {
                        case Protocol.ClientPacketType.PlanMoves:
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
                                    KickPeer(peer, $"Invalid entity id in {nameof(Protocol.ClientPacketType.PlanMoves)} packet.");
                                    return;
                                }

                                if (entity.PlayerIndex != peer.Player.PlayerIndex)
                                {
                                    KickPeer(peer, $"Can't move entity not owned in {nameof(Protocol.ClientPacketType.PlanMoves)} packet.");
                                    return;
                                }

                                entity.UpcomingMove = move;
                            }
                            */
                            break;
                    }

                    break;
            }
        }

        void ReadHelloPacket(Peer peer)
        {
            string versionString;
            try { versionString = _packetReader.ReadByteSizeString(); }
            catch { KickPeer(peer, "Invalid version string in {nameof(Protocol.ClientPacketType.Hello)} packet."); return; }
            if (versionString != Protocol.VersionString) { KickPeer(peer, $"Invalid version string, expected {Protocol.VersionString}, got {versionString}."); return; }

            Guid guid;
            try { guid = new Guid(_packetReader.ReadBytes(16)); if (guid == Guid.Empty) throw new Exception(); }
            catch { KickPeer(peer, $"Invalid player identity in {nameof(Protocol.ClientPacketType.Hello)} packet."); return; }

            string name;
            try { name = _packetReader.ReadByteSizeString(); }
            catch { KickPeer(peer, $"Invalid peer name in {nameof(Protocol.ClientPacketType.Hello)} packet."); return; }
            if (!Protocol.PlayerNameRegex.IsMatch(name)) { KickPeer(peer, $"Invalid player name: {name}."); return; }
            if (_playerIdentities.Any(x => x.Guid != guid && x.Name == name)) { KickPeer(peer, $"Name already in use."); return; }

            peer.Identity = _playerIdentities.Find(x => x.Guid == guid);

            if (peer.Identity != null)
            {
                if (peer.Identity.IsOnline) { KickPeer(peer, $"There is already someone connected with that player identity."); return; }
                peer.Identity.IsOnline = true;
            }
            else
            {
                peer.Identity = new PlayerIdentity { Guid = guid, IsOnline = true };
                _playerIdentities.Add(peer.Identity);
            }

            if (_hostGuid == Guid.Empty)
            {
                // This is the first player to successfully connect, make them host
                _hostGuid = peer.Identity.Guid;
                peer.Identity.IsHost = true;
            }

            peer.Identity.Name = name;
            _unindentifiedPeerSockets.Remove(peer.Socket);
            _identifiedPeerSockets.Add(peer.Socket);

            // Reply
            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Welcome);
            _packetWriter.WriteByte((byte)_stage);

            switch (_stage)
            {
                case ServerStage.Lobby:
                    _packetWriter.WriteByte((byte)_savedGameEntries.Count);

                    foreach (var entry in _savedGameEntries)
                    {
                        throw new NotImplementedException();
                    }

                    _packetWriter.WriteByte((byte)_scenarioEntries.Count);
                    foreach (var entry in _scenarioEntries)
                    {
                        throw new NotImplementedException();
                    }
                    break;

                case ServerStage.Playing:
                    throw new NotImplementedException();
            }

            Send(peer.Socket);
        }

        void ReadChatPacket(Peer peer)
        {
            throw new NotImplementedException();
        }
    }
}
