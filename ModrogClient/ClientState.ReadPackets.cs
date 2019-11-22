using ModrogCommon;
using SwarmBasics.Math;
using SwarmBasics.Packets;
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
                WorldChunks.Clear();
                FogChunks.Clear();
                EntitiesInSight.Clear();
                EntitiesInSightById.Clear();

                SelfPlayerIndex = _packetReader.ReadInt();
                ReadUniverseSetup();
                ReadSelfLocation();
                ReadNewEntitiesInSight();
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

            TileSize = _packetReader.ReadShort();

            // Tile kinds
            for (var layer = 0; layer < (int)ModrogApi.MapLayer.Count; layer++)
            {
                var tileKindsCount = _packetReader.ReadShort();
                TileKindsByLayer[layer] = new Game.ClientTileKind[tileKindsCount];

                for (var i = 0; i < tileKindsCount; i++)
                {
                    var spriteLocation = _packetReader.ReadShortPoint();
                    TileKindsByLayer[layer][i] = new Game.ClientTileKind(spriteLocation);
                }
            }

            var characterKindsCount = _packetReader.ReadShort();
            for (var i = 0; i < characterKindsCount; i++)
            {
                var characterKind = new Game.ClientCharacterKind(i, _packetReader.ReadShortPoint(), _packetReader.ReadShort());
                CharacterKinds.Add(characterKind);
            }

            var itemKindsCount = _packetReader.ReadShort();
            for (var i = 0; i < itemKindsCount; i++)
            {
                var itemKind = new Game.ClientItemKind(i, _packetReader.ReadShortPoint());
                ItemKinds.Add(itemKind);
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
            TickElapsedTime = 0f;

            var wasTeleported = _packetReader.ReadByte() != 0;
            if (wasTeleported)
            {
                WorldChunks.Clear();
                EntitiesInSight.Clear();
                EntitiesInSightById.Clear();

                ReadSelfLocation();
            }

            FogChunks.Clear();

            var outOfSightEntitiesCount = (int)_packetReader.ReadShort();
            for (var i = 0; i < outOfSightEntitiesCount; i++)
            {
                var id = _packetReader.ReadInt();
                EntitiesInSightById.Remove(id, out var entity);
                EntitiesInSight.Remove(entity);
            }

            ReadNewEntitiesInSight();

            if (SelectedEntity != null) EntitiesInSightById.TryGetValue(SelectedEntity.Id, out SelectedEntity);
            foreach (var entity in EntitiesInSight) entity.ClearPreviousTick();

            var dirtyEntitiesCount = (int)_packetReader.ReadShort();

            for (var i = 0; i < dirtyEntitiesCount; i++)
            {
                var id = _packetReader.ReadInt();

                var action = (ModrogApi.EntityAction)_packetReader.ReadByte();
                var direction = (ModrogApi.Direction)_packetReader.ReadByte();
                var actionItemKindId = (int)_packetReader.ReadShort();

                var entity = EntitiesInSightById[id];

                var dirtyFlags = _packetReader.ReadInt<Protocol.EntityDirtyFlags>();
                if (dirtyFlags.HasFlag(Protocol.EntityDirtyFlags.Health)) entity.Health = _packetReader.ReadShort();

                if (entity.PlayerIndex == SelfPlayerIndex)
                {
                    if (dirtyFlags.HasFlag(Protocol.EntityDirtyFlags.ItemSlots))
                    {
                        for (var j = 0; j < entity.ItemSlots.Length; j++)
                        {
                            var slotItemKindId = _packetReader.ReadShort();
                            entity.ItemSlots[j] = slotItemKindId != -1 ? ItemKinds[slotItemKindId] : null;
                        }
                    }
                }

                entity.ApplyTick(action, direction, actionItemKindId != -1 ? ItemKinds[actionItemKindId] : null);
            }

            var tileStacksCount = (int)_packetReader.ReadShort();
            for (var i = 0; i < tileStacksCount; i++)
            {
                var worldTileCoords = _packetReader.ReadShortPoint();

                var chunkCoords = new Point(
                    (int)MathF.Floor((float)worldTileCoords.X / MapChunkSide),
                    (int)MathF.Floor((float)worldTileCoords.Y / MapChunkSide));

                if (!WorldChunks.TryGetValue(chunkCoords, out var worldChunk))
                {
                    worldChunk = new Chunk((int)ModrogApi.MapLayer.Count);
                    WorldChunks.Add(chunkCoords, worldChunk);
                }

                var chunkTileCoords = new Point(
                    MathHelper.Mod(worldTileCoords.X, MapChunkSide),
                    MathHelper.Mod(worldTileCoords.Y, MapChunkSide));

                var tileOffset = chunkTileCoords.Y * MapChunkSide + chunkTileCoords.X;

                for (var layer = 0; layer < (int)ModrogApi.MapLayer.Count; layer++) worldChunk.TilesPerLayer[layer][tileOffset] = _packetReader.ReadShort();

                if (!FogChunks.TryGetValue(chunkCoords, out var fogChunk))
                {
                    fogChunk = new Chunk(1);
                    FogChunks.Add(chunkCoords, fogChunk);
                }

                fogChunk.TilesPerLayer[0][tileOffset] = 1;
            }

            _app.PlayingView.OnTick();
        }

        void ReadSelfLocation()
        {
            var location = _packetReader.ReadShortPoint();
            _app.PlayingView.OnTeleported(location);
        }

        void ReadNewEntitiesInSight()
        {
            var newlySeenEntitiesCount = (int)_packetReader.ReadShort();
            for (var i = 0; i < newlySeenEntitiesCount; i++)
            {
                var id = _packetReader.ReadInt();
                var position = _packetReader.ReadShortPoint();

                var characterKindId = _packetReader.ReadShort();
                var itemKindId = _packetReader.ReadShort();

                Game.ClientEntity entity;
                if (characterKindId != -1)
                {
                    var health = _packetReader.ReadShort();
                    var playerIndex = _packetReader.ReadByte();
                    entity = new Game.ClientEntity(id, position, CharacterKinds[characterKindId], health, playerIndex);

                    if (playerIndex == SelfPlayerIndex)
                    {
                        for (var j = 0; j < entity.ItemSlots.Length; j++)
                        {
                            var slotItemKindId = _packetReader.ReadShort();
                            if (slotItemKindId != -1) entity.ItemSlots[j] = ItemKinds[slotItemKindId];
                        }
                    }
                }
                else
                {
                    entity = new Game.ClientEntity(id, position, ItemKinds[itemKindId]);
                }

                EntitiesInSight.Add(entity);
                EntitiesInSightById.Add(entity.Id, entity);
            }
        }
        #endregion
    }
}
