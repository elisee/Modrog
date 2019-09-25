using DeepSwarmCommon;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace DeepSwarmClient
{
    partial class Engine
    {
        void SendPacket()
        {
            try { _socket.Send(_writer.Buffer, 0, _writer.Finish(), SocketFlags.None); } catch { }
        }

        void ReadPlayerList()
        {
            PlayerList.Clear();

            var playerCount = _reader.ReadInt();

            for (var i = 0; i < playerCount; i++)
            {
                var name = _reader.ReadByteSizeString();
                var team = (Player.PlayerTeam)_reader.ReadByte();
                var isOnline = _reader.ReadByte() != 0;

                PlayerList.Add(new PlayerListEntry { Name = name, Team = team, IsOnline = isOnline });
            }

            InGameView.OnPlayerListUpdated();
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

            _tickIndex = _reader.ReadInt();

            // Read seen entities
            Entity newSelectedEntity = null;

            var seenEntitiesCount = _reader.ReadShort();
            for (var i = 0; i < seenEntitiesCount; i++)
            {
                var entity = new Entity
                {
                    Id = _reader.ReadInt(),
                    X = _reader.ReadShort(),
                    Y = _reader.ReadShort(),
                    PlayerIndex = _reader.ReadShort(),
                    Type = (Entity.EntityType)_reader.ReadByte(),
                    Direction = (Entity.EntityDirection)_reader.ReadByte(),
                    Health = _reader.ReadByte(),
                    Crystals = _reader.ReadInt(),
                };

                if (SelectedEntity?.Id == entity.Id) newSelectedEntity = entity;

                Map.Entities.Add(entity);
                Map.EntitiesById.Add(entity.Id, entity);

                if (entity.PlayerIndex == SelfState.PlayerIndex && entity.Type == Entity.EntityType.Factory)
                {
                    SelfState.BaseChunkX = entity.X / Map.ChunkSize;
                    SelfState.BaseChunkY = entity.Y / Map.ChunkSize;
                }
            }

            SelectedEntity = newSelectedEntity;
            if (SelectedEntity != null) InGameView.OnSelectedEntityUpdated();

            // Read seen tiles
            var seenTilesCount = _reader.ReadShort();
            for (var i = 0; i < seenTilesCount; i++)
            {
                var x = _reader.ReadShort();
                var y = _reader.ReadShort();
                FogOfWar[y * Map.MapSize + x] = 1;
                Map.Tiles[y * Map.MapSize + x] = _reader.ReadByte();
            }

            // Collect planned moves from keyboard and scripting
            var plannedMoves = new Dictionary<int, Entity.EntityMove>();

            if (SelectedEntity != null && _selectedEntityMoveDirection != null)
            {
                plannedMoves[SelectedEntity.Id] = SelectedEntity.GetMoveForTargetDirection(_selectedEntityMoveDirection.Value);
            }

            var removedSelfEntityIds = new List<int>();

            foreach (var (entityId, lua) in _luasByEntityId)
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
                _luasByEntityId.Remove(entityId);
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
            _writer.WriteByte((byte)Protocol.ClientPacketType.PlanMoves);
            _writer.WriteInt(_tickIndex);
            _writer.WriteShort((short)plannedMoves.Count);
            foreach (var (entityId, move) in plannedMoves)
            {
                _writer.WriteInt(entityId);
                _writer.WriteByte((byte)move);
            }
            SendPacket();
        }
    }
}
