using Microsoft.CodeAnalysis;
using ModrogCommon;
using SwarmBasics.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ModrogServer
{
    partial class ServerState
    {
        void ReadPacket(Peer peer)
        {
            var packetType = (Protocol.ClientPacketType)_packetReader.ReadByte();

            if (peer.Identity == null)
            {
                if (packetType != Protocol.ClientPacketType.Hello) throw new PacketException($"Invalid packet type, expected {nameof(Protocol.ClientPacketType.Hello)}, got {packetType}.");
                Console.WriteLine($"{peer.Socket.RemoteEndPoint} - Got {nameof(Protocol.ClientPacketType.Hello)} packet.");

                ReadHello(peer);
                return;
            }

            if (packetType == Protocol.ClientPacketType.Chat) { ReadChat(peer); return; }

            switch (_stage)
            {
                case ServerStage.Lobby:
                    switch (packetType)
                    {
                        case Protocol.ClientPacketType.SetScenario: ReadSetScenario(peer); break;
                        case Protocol.ClientPacketType.Ready: ReadSetReady(peer); break;
                        case Protocol.ClientPacketType.StartGame: ReadStartGame(peer); break;
                        default:
                            throw new PacketException($"Invalid packet type for {nameof(ServerStage.Lobby)} stage: {packetType}.");
                    }
                    break;

                case ServerStage.CountingDown:
                    switch (packetType)
                    {
                        case Protocol.ClientPacketType.StopGame: ReadStopGame(peer); break;
                    }
                    break;

                case ServerStage.Playing:
                    switch (packetType)
                    {
                        case Protocol.ClientPacketType.SetPlayerPosition: ReadPlayerPosition(peer); break;
                        case Protocol.ClientPacketType.SetEntityIntents: ReadSetEntityIntents(peer); break;
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
            if (_peerIdentities.Any(x => x.Guid != guid && x.Name == name)) throw new PacketException($"Name already in use.");

            peer.Identity = _peerIdentities.Find(x => x.Guid == guid);

            if (peer.Identity != null)
            {
                if (peer.Identity.IsOnline) throw new PacketException($"There is already someone connected with that player identity.");
                peer.Identity.IsOnline = true;

                if (_stage != ServerStage.Lobby)
                {
                    _packetWriter.WriteByte((byte)Protocol.ServerPacketType.SetPeerOnline);
                    _packetWriter.WriteByteSizeString(peer.Identity.Name);
                    _packetWriter.WriteByte(1);
                    Broadcast();
                }
            }
            else
            {
                if (_stage != ServerStage.Lobby) throw new PacketException("New player cannot join outside of lobby.");

                peer.Identity = new PeerIdentity { Guid = guid, IsOnline = true };
                _peerIdentities.Add(peer.Identity);
            }

            if (_hostGuid == Guid.Empty || _hostGuid == peer.Identity.Guid)
            {
                // This is the first player to successfully connect, make them host & ready
                _hostGuid = peer.Identity.Guid;
                peer.Identity.IsHost = true;
                peer.Identity.IsReady = true;
            }

            peer.Identity.Name = name;
            _unindentifiedPeerSockets.Remove(peer.Socket);
            _identifiedPeerSockets.Add(peer.Socket);

            BroadcastPeerList();

            // Reply
            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Welcome);
            _packetWriter.WriteByte((byte)_stage);

            _packetWriter.WriteByte((byte)_scenarioEntries.Count);
            foreach (var entry in _scenarioEntries)
            {
                _packetWriter.WriteByteSizeString(entry.Name);
                _packetWriter.WriteByteSizeString(entry.Title);
                _packetWriter.WriteByte((byte)entry.MinPlayers);
                _packetWriter.WriteByte((byte)entry.MaxPlayers);
                _packetWriter.WriteByte((byte)(entry.SupportsCoop ? 1 : 0));
                _packetWriter.WriteByte((byte)(entry.SupportsVersus ? 1 : 0));
                _packetWriter.WriteShortSizeString(entry.Description);
            }

            /*
            _packetWriter.WriteByte((byte)_savedGameEntries.Count);

            foreach (var entry in _savedGameEntries)
            {
                throw new NotImplementedException();
            }
            */

            _packetWriter.WriteByteSizeString(_scenarioName ?? "");

            switch (_stage)
            {
                case ServerStage.Lobby:
                    break;

                case ServerStage.Playing:
                    var player = _universe.Players[peer.Identity.PlayerIndex];

                    _packetWriter.WriteInt(player.Index);
                    WriteUniverseSetup();
                    _packetWriter.WriteShortPoint(player.Position);
                    WriteNewEntitiesInSight(player.EntitiesInSight, player.Index);
                    break;
            }

            Send(peer.Socket);
        }

        #region Lobby and Playing Stages
        void ReadChat(Peer peer)
        {
            var text = _packetReader.ReadByteSizeString();
            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Chat);
            _packetWriter.WriteByteSizeString(peer.Identity.Name);
            _packetWriter.WriteByteSizeString(text);
            Broadcast();
        }
        #endregion

        #region Lobby Stage
        void ReadSetScenario(Peer peer)
        {
            if (!peer.Identity.IsHost) throw new PacketException("Can't choose game if not host.");

            var scenarioName = _packetReader.ReadByteSizeString();
            var scenarioEntry = _scenarioEntries.Find(x => x.Name == scenarioName) ?? throw new PacketException($"Unknown scenario: {scenarioName}.");
            _scenarioName = scenarioEntry.Name;

            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.SetScenario);
            _packetWriter.WriteByteSizeString(_scenarioName);
            Broadcast();
        }

        void ReadSetReady(Peer peer)
        {
            peer.Identity.IsReady = _packetReader.ReadByte() != 0;
            BroadcastPeerList();
        }

        void ReadStartGame(Peer peer)
        {
            if (!peer.Identity.IsHost) throw new PacketException("Can't start game if not host.");

            void SendChatError(string message)
            {
                _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Chat);
                _packetWriter.WriteByteSizeString("");
                _packetWriter.WriteByteSizeString(message);
                Send(peer.Socket);
            }

            if (_scenarioName == null) { SendChatError("You must select a scenario to play before starting the game."); return; }

            var scenarioEntry = _scenarioEntries.Find(x => x.Name == _scenarioName);
            if (_peerIdentities.Count < scenarioEntry.MinPlayers) { SendChatError($"Cannot start game, not enough players for this scenario (minimum is {scenarioEntry.MinPlayers})."); return; }
            if (_peerIdentities.Count > scenarioEntry.MaxPlayers) { SendChatError($"Cannot start game, too many players for this scenario (maximum is {scenarioEntry.MaxPlayers})."); return; }
            if (_peerIdentities.Any(x => !x.IsReady)) { SendChatError("Cannot start game, not all players are ready."); return; }

            // TODO: Check if team configurations are okay once that is implemented

            // Skip countdown is there is a single player
            if (_peerIdentities.Count == 1)
            {
                StartPlaying();
                return;
            }

            _stage = ServerStage.CountingDown;
            _startCountdownTimer = 0f;
            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.SetupCountdown);
            _packetWriter.WriteByte(1);
            Broadcast();
        }

        void ReadStopGame(Peer peer)
        {
            if (!peer.Identity.IsHost) throw new PacketException("Can't stop game if not host.");

            if (_stage == ServerStage.Playing)
            {
                _universe.Dispose();
                _universe = null;
            }

            _stage = ServerStage.Lobby;
            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.SetupCountdown);
            _packetWriter.WriteByte(0);
            Broadcast();

            _peerIdentities.RemoveAll(x => !x.IsOnline);
            BroadcastPeerList();
        }
        #endregion

        #region Playing Stage
        void WriteNewEntitiesInSight(ICollection<Game.InternalEntity> entities, int playerIndex)
        {
            _packetWriter.WriteShort((short)entities.Count);
            foreach (var entity in entities)
            {
                _packetWriter.WriteInt(entity.Id);
                // For characters, we send the previous tick position so the client can interpolate after applying the action below
                _packetWriter.WriteShortPoint(entity.CharacterKind != null ? entity.PreviousTickPosition : entity.Position);

                _packetWriter.WriteShort((short)(entity.CharacterKind?.Id ?? -1));
                _packetWriter.WriteShort((short)(entity.ItemKind?.Id ?? -1));

                if (entity.CharacterKind != null)
                {
                    _packetWriter.WriteShort((short)entity.Health);
                    _packetWriter.WriteByte((byte)entity.PlayerIndex);

                    if (entity.PlayerIndex == playerIndex)
                    {
                        foreach (var itemKind in entity.ItemSlots) _packetWriter.WriteShort((short)(itemKind?.Id ?? -1));
                    }
                }
            }
        }

        void ReadPlayerPosition(Peer peer)
        {
            var player = _universe.Players[peer.Identity.PlayerIndex];
            player.Position = _packetReader.ReadShortPoint();
        }

        void ReadSetEntityIntents(Peer peer)
        {
            var clientTickIndex = _packetReader.ReadInt();
            if (clientTickIndex != _universe.TickIndex)
            {
                Console.WriteLine($"{peer.Socket.RemoteEndPoint} - Ignoring {nameof(Protocol.ClientPacketType.SetEntityIntents)} packet from tick {clientTickIndex}, we're at {_universe.TickIndex}.");
                return;
            }

            var player = _universe.Players[peer.Identity.PlayerIndex];

            var intentCount = _packetReader.ReadShort();
            for (var i = 0; i < intentCount; i++)
            {
                var entityId = _packetReader.ReadInt();
                if (!player.OwnedEntitiesById.TryGetValue(entityId, out var entity)) throw new PacketException($"Invalid entity id in {nameof(Protocol.ClientPacketType.SetEntityIntents)} packet.");

                var intent = _packetReader.ReadByte<ModrogApi.CharacterIntent>();

                var intentDirection = ModrogApi.Direction.Down;
                if (intent == ModrogApi.CharacterIntent.Move || intent == ModrogApi.CharacterIntent.Use) intentDirection = _packetReader.ReadByte<ModrogApi.Direction>();

                var intentSlot = 0;
                if (intent == ModrogApi.CharacterIntent.Use || intent == ModrogApi.CharacterIntent.Swap) intentSlot = _packetReader.ReadByte(max: 1);

                entity.Intent = intent;
                entity.IntentDirection = intentDirection;
                entity.IntentSlot = intentSlot;
            }
        }
        #endregion
    }
}
