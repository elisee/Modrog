using DeepSwarmCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static DeepSwarmCommon.Protocol;


namespace DeepSwarmClient
{
    partial class ClientState
    {
        void ReadPackets(List<byte[]> packets)
        {
            void Abort(string reason)
            {
                Trace.WriteLine($"Abort: {reason}");
                Stop();
            }

            foreach (var packet in packets)
            {
                _packetReader.Open(packet);
                if (!IsRunning) break;

                try
                {
                    var packetType = (Protocol.ServerPacketType)_packetReader.ReadByte();

                    bool EnsureView(EngineView view)
                    {
                        if (View == view) return true;
                        Abort($"Received packet {packetType} during wrong stage (expected {view} but in {View}.");
                        return false;
                    }

                    bool EnsureLobbyOrPlayingView()
                    {
                        if (View == EngineView.Lobby || View == EngineView.Playing) return true;
                        Abort($"Received packet {packetType} in wrong view (expected Lobby or Playing but in {View}.");
                        return false;
                    }

                    switch (packetType)
                    {
                        case ServerPacketType.Welcome:
                            if (!EnsureView(EngineView.Loading)) break;

                            ReadWelcome();
                            break;

                        case ServerPacketType.PlayerList:
                            ReadPlayerList();
                            break;

                        case ServerPacketType.Chat:
                            if (!EnsureLobbyOrPlayingView()) break;
                            ReadChat();
                            break;

                        case ServerPacketType.Tick:
                            if (!EnsureLobbyOrPlayingView()) break;

                            ReadTick();

                            if (View == EngineView.Lobby)
                            {
                                View = EngineView.Playing;
                                _engine.Interface.OnViewChanged();
                            }

                            break;
                    }
                }
                catch (PacketException packetException)
                {
                    Abort(packetException.Message);
                }
            }
        }

        void ReadWelcome()
        {
            var isPlaying = _packetReader.ReadByte() != 0;
            View = isPlaying ? EngineView.Playing : EngineView.Lobby;

            if (!isPlaying)
            {
                var scenarioCount = _packetReader.ReadByte();
                ScenarioEntries.Clear();
                for (var i = 0; i < scenarioCount; i++)
                {
                    ScenarioEntries.Add(new ScenarioEntry
                    {
                        Name = _packetReader.ReadByteSizeString(),
                        MinPlayers = _packetReader.ReadByte(),
                        MaxPlayers = _packetReader.ReadByte(),
                        SupportedModes = (ScenarioEntry.ScenarioMode)_packetReader.ReadByte(),
                        Description = _packetReader.ReadShortSizeString()
                    });
                }

                var savedGamesCount = _packetReader.ReadByte();
                SavedGameEntries.Clear();
                for (var i = 0; i < savedGamesCount; i++)
                {
                    throw new NotImplementedException();
                }
            }
            else
            {

            }

            _engine.Interface.OnViewChanged();
        }

        void ReadPlayerList()
        {
            PlayerList.Clear();

            var playerCount = _packetReader.ReadInt();

            for (var i = 0; i < playerCount; i++)
            {
                var name = _packetReader.ReadByteSizeString();
                var flags = _packetReader.ReadByte();
                var isHost = (flags & 1) != 0;
                var isOnline = (flags & 2) != 0;
                var isReady = (flags & 4) != 0;

                PlayerList.Add(new PlayerListEntry { Name = name, IsHost = isHost, IsOnline = isOnline, IsReady = isReady });
            }

            switch (View)
            {
                case EngineView.Playing: _engine.Interface.PlayingView.OnPlayerListUpdated(); break;
                case EngineView.Lobby: _engine.Interface.LobbyView.OnPlayerListUpdated(); break;
            }
        }

        void ReadChat()
        {
            // TODO
        }

        void ReadTick()
        {
            Unsafe.InitBlock(ref FogOfWar[0], 0, (uint)FogOfWar.Length);
            Map.Entities.Clear();
            Map.EntitiesById.Clear();

            TickIndex = _packetReader.ReadInt();

            // Read seen entities
            Entity newSelectedEntity = null;

            var seenEntitiesCount = _packetReader.ReadShort();
            for (var i = 0; i < seenEntitiesCount; i++)
            {
                var entity = new Entity
                {
                    Id = _packetReader.ReadInt(),
                    X = _packetReader.ReadShort(),
                    Y = _packetReader.ReadShort(),
                    PlayerIndex = _packetReader.ReadShort(),
                    Type = (Entity.EntityType)_packetReader.ReadByte(),
                    Direction = (Entity.EntityDirection)_packetReader.ReadByte(),
                    Health = _packetReader.ReadByte(),
                    Crystals = _packetReader.ReadInt(),
                };

                if (SelectedEntity?.Id == entity.Id) newSelectedEntity = entity;

                Map.Entities.Add(entity);
                Map.EntitiesById.Add(entity.Id, entity);

                if (entity.PlayerIndex == SelfPlayerIndex && entity.Type == Entity.EntityType.Factory)
                {
                    SelfBaseChunkX = entity.X / Map.ChunkSize;
                    SelfBaseChunkY = entity.Y / Map.ChunkSize;
                }
            }

            SelectedEntity = newSelectedEntity;
            if (SelectedEntity != null) _engine.Interface.PlayingView.OnSelectedEntityUpdated();

            // Read seen tiles
            var seenTilesCount = _packetReader.ReadShort();
            for (var i = 0; i < seenTilesCount; i++)
            {
                var x = _packetReader.ReadShort();
                var y = _packetReader.ReadShort();
                FogOfWar[y * Map.MapSize + x] = 1;
                Map.Tiles[y * Map.MapSize + x] = _packetReader.ReadByte();
            }

            // Collect planned moves from keyboard and scripting
            var plannedMoves = new Dictionary<int, Entity.EntityMove>();

            if (SelectedEntity != null && SelectedEntityMoveDirection != null)
            {
                plannedMoves[SelectedEntity.Id] = SelectedEntity.GetMoveForTargetDirection(SelectedEntityMoveDirection.Value);
            }

            var removedSelfEntityIds = new List<int>();

            foreach (var (entityId, lua) in LuasByEntityId)
            {
                if (!Map.EntitiesById.TryGetValue(entityId, out var entity))
                {
                    lua.Dispose();
                    removedSelfEntityIds.Add(entityId);
                    continue;
                }

                RunScriptOnEntity(lua, entity);
            }

            foreach (var entityId in removedSelfEntityIds)
            {
                EntityScriptPaths.Remove(entityId);
                LuasByEntityId.Remove(entityId);
            }

            void RunScriptOnEntity(KeraLua.Lua lua, Entity entity)
            {
                var type = lua.GetGlobal("tick");
                if (type != KeraLua.LuaType.Function)
                {
                    // TODO: Display error in UI / on entity as an icon
                    Trace.WriteLine("There must be a tick function.");
                    lua.Pop(1);
                    return;
                }

                lua.NewTable();

                lua.PushString("forward");
                lua.PushCFunction((_) =>
                {
                    plannedMoves[entity.Id] = Entity.EntityMove.Forward;
                    return 0;
                });
                lua.RawSet(-3);

                lua.PushString("rotateCW");
                lua.PushCFunction((_) => { plannedMoves[entity.Id] = Entity.EntityMove.RotateCW; return 0; });
                lua.RawSet(-3);

                lua.PushString("rotateCCW");
                lua.PushCFunction((_) => { plannedMoves[entity.Id] = Entity.EntityMove.RotateCCW; return 0; });
                lua.RawSet(-3);

                lua.PushString("build");
                lua.PushCFunction((_) => { plannedMoves[entity.Id] = Entity.EntityMove.Build; return 0; });
                lua.RawSet(-3);

                var status = lua.PCall(1, 0, 0);
                if (status != KeraLua.LuaStatus.OK)
                {
                    // TODO: Display error in UI / on entity as an icon
                    Trace.WriteLine($"Error running script: {status}");
                    var error = lua.ToString(-1);
                    Trace.WriteLine(error);
                }
            }

            // Send planned moves
            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.PlanMoves);
            _packetWriter.WriteInt(TickIndex);
            _packetWriter.WriteShort((short)plannedMoves.Count);
            foreach (var (entityId, move) in plannedMoves)
            {
                _packetWriter.WriteInt(entityId);
                _packetWriter.WriteByte((byte)move);
            }
            SendPacket();
        }
    }
}
