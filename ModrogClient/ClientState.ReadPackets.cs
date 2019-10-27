﻿using ModrogApi;
using ModrogCommon;
using SwarmBasics.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static ModrogCommon.Protocol;


namespace ModrogClient
{
    partial class ClientState
    {
        void ReadPackets(List<byte[]> packets)
        {
            void Abort(string reason)
            {
                Trace.WriteLine($"Aborted: {reason}");
                Disconnect(error: $"Aborted: {reason}");
            }

            foreach (var packet in packets)
            {
                _packetReader.Open(packet);
                if (Stage == ClientStage.Exited) break;

                try
                {
                    var packetType = (ServerPacketType)_packetReader.ReadByte();

                    bool EnsureStage(ClientStage view)
                    {
                        if (Stage == view) return true;
                        Abort($"Received packet {packetType} during wrong stage (expected {view} but in {Stage}.");
                        return false;
                    }

                    bool EnsureLobbyOrPlayingView()
                    {
                        if (Stage == ClientStage.Lobby || Stage == ClientStage.Playing) return true;
                        Abort($"Received packet {packetType} in wrong view (expected Lobby or Playing but in {Stage}.");
                        return false;
                    }

                    switch (packetType)
                    {
                        case ServerPacketType.Kick:
                            KickReason = _packetReader.ReadByteSizeString();
                            break;

                        case ServerPacketType.Welcome:
                            if (!EnsureStage(ClientStage.Loading)) break;

                            ReadWelcome();
                            break;

                        case ServerPacketType.PeerList: ReadPeerList(); break;
                        case ServerPacketType.Chat: ReadChat(); break;

                        case ServerPacketType.SetScenario:
                            if (!EnsureStage(ClientStage.Lobby)) break;
                            ReadSetScenario();
                            break;

                        case ServerPacketType.SetupCountdown:
                            if (!EnsureStage(ClientStage.Lobby)) break;
                            ReadSetupCountdown();
                            break;

                        case ServerPacketType.SetPlayerIndex:
                            if (!EnsureStage(ClientStage.Lobby)) break;
                            ReadSetPlayerIndex();
                            break;

                        case ServerPacketType.UniverseSetup:
                            if (!EnsureStage(ClientStage.Lobby)) break;
                            ReadUniverseSetup();
                            break;

                        case ServerPacketType.SetPeerOnline:
                            if (!EnsureStage(ClientStage.Playing)) break;
                            throw new NotImplementedException();

                        case ServerPacketType.Tick:
                            if (!EnsureLobbyOrPlayingView()) break;

                            ReadTick();
                            break;
                    }
                }
                catch (PacketException packetException)
                {
                    Abort(packetException.Message);
                }
            }
        }

        #region Loading Stage
        void ReadWelcome()
        {
            var isPlaying = _packetReader.ReadByte() != 0;
            Stage = isPlaying ? ClientStage.Playing : ClientStage.Lobby;

            var scenarioCount = _packetReader.ReadByte();
            ScenarioEntries.Clear();
            for (var i = 0; i < scenarioCount; i++)
            {
                ScenarioEntries.Add(new ScenarioEntry
                {
                    Name = _packetReader.ReadByteSizeString(),
                    Title = _packetReader.ReadByteSizeString(),
                    MinPlayers = _packetReader.ReadByte(),
                    MaxPlayers = _packetReader.ReadByte(),
                    SupportsCoop = _packetReader.ReadByte() != 0,
                    SupportsVersus = _packetReader.ReadByte() != 0,
                    Description = _packetReader.ReadShortSizeString()
                });
            }


            /*
            var savedGamesCount = _packetReader.ReadByte();
            SavedGameEntries.Clear();
            for (var i = 0; i < savedGamesCount; i++)
            {
                throw new NotImplementedException();
            }
            */

            ReadSetScenario();

            if (!isPlaying)
            {
                if (_preselectedScenario != null && ScenarioEntries.Any(x => x.Name == _preselectedScenario))
                {
                    SetScenario(_preselectedScenario);
                    ToggleReady();
                    StartGame();
                }
            }
            else
            {
                SelfPlayerIndex = _packetReader.ReadInt();

                WorldChunks.Clear();
                FogChunks.Clear();
                SeenEntities.Clear();

                ReadUniverseSetup();
            }

            _app.OnStageChanged();
        }
        #endregion

        #region Lobby or Playing Stage
        void ReadChat()
        {
            var author = _packetReader.ReadByteSizeString();
            var message = _packetReader.ReadByteSizeString();

            switch (Stage)
            {
                case ClientStage.Lobby: _app.LobbyView.OnChatMessageReceived(author, message); break;
                case ClientStage.Playing: _app.PlayingView.OnChatMessageReceived(author, message); break;
            }
        }
        #endregion

        #region Lobby Stage
        void ReadPeerList()
        {
            PlayerList.Clear();

            var playerCount = _packetReader.ReadInt();

            for (var i = 0; i < playerCount; i++)
            {
                var name = _packetReader.ReadByteSizeString();
                var flags = _packetReader.ReadByte();
                var playerId = (int)_packetReader.ReadByte();

                PlayerList.Add(new PeerIdentity
                {
                    Name = name,
                    IsHost = (flags & 1) != 0,
                    IsOnline = (flags & 2) != 0,
                    IsReady = (flags & 4) != 0,
                    PlayerId = playerId
                });
            }

            switch (Stage)
            {
                case ClientStage.Lobby: _app.LobbyView.OnPlayerListUpdated(); break;
                case ClientStage.Playing: _app.PlayingView.OnPlayerListUpdated(); break;
            }
        }

        void ReadSetScenario()
        {
            var scenarioName = _packetReader.ReadByteSizeString();
            ActiveScenario = ScenarioEntries.Find(x => x.Name == scenarioName);
            _app.LobbyView.OnActiveScenarioChanged();
        }

        void ReadSetupCountdown()
        {
            IsCountingDown = _packetReader.ReadByte() != 0;
            _app.LobbyView.OnIsCountingDownChanged();
        }

        void ReadSetPlayerIndex()
        {
            SelfPlayerIndex = _packetReader.ReadInt();
        }

        void ReadUniverseSetup()
        {
            // Spritesheet
            var size = _packetReader.ReadInt();
            var image = _packetReader.ReadBytes(size);
            _app.PlayingView.OnSpritesheetReceived(image);

            // Tile kinds
            for (var layer = 0; layer < (int)MapLayer.Count; layer++)
            {
                var tileKindsCount = _packetReader.ReadInt();
                TileKindsByLayer[layer] = new Game.ClientTileKind[tileKindsCount];

                for (var i = 0; i < tileKindsCount; i++)
                {
                    var spriteLocation = new Point(_packetReader.ReadShort(), _packetReader.ReadShort());
                    TileKindsByLayer[layer][i] = new Game.ClientTileKind(spriteLocation);
                }
            }
        }
        #endregion

        #region Playing Stage
        void ReadTick()
        {
            if (Stage == ClientStage.Lobby)
            {
                Stage = ClientStage.Playing;
                _app.OnStageChanged();
            }

            TickIndex = _packetReader.ReadInt();
            SeenEntities.Clear();

            var wasTeleported = _packetReader.ReadByte() != 0;
            if (wasTeleported)
            {
                WorldChunks.Clear();

                var location = new Point(_packetReader.ReadShort(), _packetReader.ReadShort());
                _app.PlayingView.OnTeleported(location);
            }

            FogChunks.Clear();

            Game.ClientEntity newSelectedEntity = null;

            var entitiesCount = (int)_packetReader.ReadShort();
            for (var i = 0; i < entitiesCount; i++)
            {
                var id = _packetReader.ReadInt();
                var spriteLocation = new Point(_packetReader.ReadShort(), _packetReader.ReadShort());
                var position = new Point(_packetReader.ReadShort(), _packetReader.ReadShort());
                var direction = (EntityDirection)_packetReader.ReadByte();
                var playerIndex = _packetReader.ReadShort();

                var entity = new Game.ClientEntity(id) { SpriteLocation = spriteLocation, Position = position, Direction = direction, PlayerIndex = playerIndex };
                SeenEntities.Add(entity);

                if (SelectedEntity?.Id == entity.Id) newSelectedEntity = entity;
            }

            SelectedEntity = newSelectedEntity;

            var tileStacksCount = (int)_packetReader.ReadShort();
            for (var i = 0; i < tileStacksCount; i++)
            {
                var worldTileCoords = new Point(_packetReader.ReadShort(), _packetReader.ReadShort());

                var chunkCoords = new Point(
                    (int)MathF.Floor((float)worldTileCoords.X / MapChunkSide),
                    (int)MathF.Floor((float)worldTileCoords.Y / MapChunkSide));

                if (!WorldChunks.TryGetValue(chunkCoords, out var worldChunk))
                {
                    worldChunk = new Chunk((int)MapLayer.Count);
                    WorldChunks.Add(chunkCoords, worldChunk);
                }

                var chunkTileCoords = new Point(
                    MathHelper.Mod(worldTileCoords.X, MapChunkSide),
                    MathHelper.Mod(worldTileCoords.Y, MapChunkSide));

                var tileOffset = chunkTileCoords.Y * MapChunkSide + chunkTileCoords.X;

                for (var layer = 0; layer < (int)MapLayer.Count; layer++) worldChunk.TilesPerLayer[layer][tileOffset] = _packetReader.ReadShort();

                if (!FogChunks.TryGetValue(chunkCoords, out var fogChunk))
                {
                    fogChunk = new Chunk(1);
                    FogChunks.Add(chunkCoords, fogChunk);
                }

                fogChunk.TilesPerLayer[0][tileOffset] = 1;
            }

            // Send scroll update
            var scrollPosition = new Point(
                (int)(_app.PlayingView.Scroll.X / Protocol.MapTileSize),
                (int)(_app.PlayingView.Scroll.Y / Protocol.MapTileSize));

            _packetWriter.WriteByte((byte)ClientPacketType.SetPosition);
            _packetWriter.WriteShort((short)scrollPosition.X);
            _packetWriter.WriteShort((short)scrollPosition.Y);
            SendPacket();

            // Send planned moves
            var plannedMoves = new Dictionary<int, EntityMove>();

            if (SelectedEntity != null && SelectedEntityMoveDirection != null)
            {
                plannedMoves[SelectedEntity.Id] = SelectedEntity.GetMoveForTargetDirection(SelectedEntityMoveDirection.Value);
            }

            _packetWriter.WriteByte((byte)ClientPacketType.PlanMoves);
            _packetWriter.WriteInt(TickIndex);
            _packetWriter.WriteShort((short)plannedMoves.Count);
            foreach (var (entityId, move) in plannedMoves)
            {
                _packetWriter.WriteInt(entityId);
                _packetWriter.WriteByte((byte)move);
            }
            SendPacket();
        }
        #endregion
    }
}
