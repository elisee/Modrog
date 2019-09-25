using DeepSwarmClient.UI;
using DeepSwarmCommon;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static DeepSwarmCommon.Protocol;


namespace DeepSwarmClient
{
    partial class Engine
    {
        void ReadPackets(List<byte[]> packets)
        {
            void Abort(string reason)
            {
                _socket.Close();
                _isRunning = false;
                Trace.WriteLine($"Abort: {reason}");
            }

            foreach (var packet in packets)
            {
                PacketReader.Open(packet);
                if (!_isRunning) break;

                try
                {
                    var packetType = (Protocol.ServerPacketType)PacketReader.ReadByte();

                    bool EnsureStage(EngineStage stage)
                    {
                        if (State.Stage == stage) return true;
                        Abort($"Received packet {packetType} during wrong stage (expected {stage} but in {State.Stage}.");
                        return false;
                    }

                    bool EnsureLoadingOrPlayingStage()
                    {
                        if (State.Stage == EngineStage.Loading || State.Stage == EngineStage.Playing) return true;
                        Abort($"Received packet {packetType} during wrong stage (expected Loading or Playing but in {State.Stage}.");
                        return false;
                    }

                    switch (packetType)
                    {
                        case ServerPacketType.SetupPlayerIndex:
                            if (!EnsureStage(EngineStage.Loading)) break;
                            State.SelfPlayerIndex = PacketReader.ReadInt();
                            break;

                        case ServerPacketType.PlayerList:
                            if (!EnsureLoadingOrPlayingStage()) break;
                            ReadPlayerList();
                            break;

                        case ServerPacketType.Chat:
                            if (!EnsureLoadingOrPlayingStage()) break;
                            ReadChat();
                            break;

                        case ServerPacketType.Tick:
                            if (!EnsureLoadingOrPlayingStage()) break;
                            if (State.SelfPlayerIndex == -1) { Abort("Received tick before receiving self player index."); break; }

                            ReadTick();

                            if (State.Stage == EngineStage.Loading)
                            {
                                Desktop.SetRootElement(InGameView);
                                Desktop.SetFocusedElement(InGameView);
                                State.Stage = EngineStage.Playing;
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

        void ReadPlayerList()
        {
            State.PlayerList.Clear();

            var playerCount = PacketReader.ReadInt();

            for (var i = 0; i < playerCount; i++)
            {
                var name = PacketReader.ReadByteSizeString();
                var team = (Player.PlayerTeam)PacketReader.ReadByte();
                var isOnline = PacketReader.ReadByte() != 0;

                State.PlayerList.Add(new PlayerListEntry { Name = name, Team = team, IsOnline = isOnline });
            }

            InGameView.OnPlayerListUpdated();
        }

        void ReadChat()
        {
            // TODO
        }

        void ReadTick()
        {
            Unsafe.InitBlock(ref State.FogOfWar[0], 0, (uint)State.FogOfWar.Length);
            State.Map.Entities.Clear();
            State.Map.EntitiesById.Clear();

            State.TickIndex = PacketReader.ReadInt();

            // Read seen entities
            Entity newSelectedEntity = null;

            var seenEntitiesCount = PacketReader.ReadShort();
            for (var i = 0; i < seenEntitiesCount; i++)
            {
                var entity = new Entity
                {
                    Id = PacketReader.ReadInt(),
                    X = PacketReader.ReadShort(),
                    Y = PacketReader.ReadShort(),
                    PlayerIndex = PacketReader.ReadShort(),
                    Type = (Entity.EntityType)PacketReader.ReadByte(),
                    Direction = (Entity.EntityDirection)PacketReader.ReadByte(),
                    Health = PacketReader.ReadByte(),
                    Crystals = PacketReader.ReadInt(),
                };

                if (State.SelectedEntity?.Id == entity.Id) newSelectedEntity = entity;

                State.Map.Entities.Add(entity);
                State.Map.EntitiesById.Add(entity.Id, entity);

                if (entity.PlayerIndex == State.SelfPlayerIndex && entity.Type == Entity.EntityType.Factory)
                {
                    State.SelfBaseChunkX = entity.X / Map.ChunkSize;
                    State.SelfBaseChunkY = entity.Y / Map.ChunkSize;
                }
            }

            State.SelectedEntity = newSelectedEntity;
            if (State.SelectedEntity != null) InGameView.OnSelectedEntityUpdated();

            // Read seen tiles
            var seenTilesCount = PacketReader.ReadShort();
            for (var i = 0; i < seenTilesCount; i++)
            {
                var x = PacketReader.ReadShort();
                var y = PacketReader.ReadShort();
                State.FogOfWar[y * Map.MapSize + x] = 1;
                State.Map.Tiles[y * Map.MapSize + x] = PacketReader.ReadByte();
            }

            // Collect planned moves from keyboard and scripting
            var plannedMoves = new Dictionary<int, Entity.EntityMove>();

            if (State.SelectedEntity != null && State.SelectedEntityMoveDirection != null)
            {
                plannedMoves[State.SelectedEntity.Id] = State.SelectedEntity.GetMoveForTargetDirection(State.SelectedEntityMoveDirection.Value);
            }

            var removedSelfEntityIds = new List<int>();

            foreach (var (entityId, lua) in State.LuasByEntityId)
            {
                if (!State.Map.EntitiesById.TryGetValue(entityId, out var entity))
                {
                    lua.Dispose();
                    removedSelfEntityIds.Add(entityId);
                    continue;
                }

                RunScriptOnEntity(lua, entity);
            }

            foreach (var entityId in removedSelfEntityIds)
            {
                State.EntityScriptPaths.Remove(entityId);
                State.LuasByEntityId.Remove(entityId);
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
            PacketWriter.WriteByte((byte)Protocol.ClientPacketType.PlanMoves);
            PacketWriter.WriteInt(State.TickIndex);
            PacketWriter.WriteShort((short)plannedMoves.Count);
            foreach (var (entityId, move) in plannedMoves)
            {
                PacketWriter.WriteInt(entityId);
                PacketWriter.WriteByte((byte)move);
            }
            SendPacket();
        }
    }
}
