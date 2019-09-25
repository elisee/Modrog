using DeepSwarmCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DeepSwarmClient
{
    enum EngineStage { EnterName, Loading, Playing }

    class EngineState
    {
        public EngineStage Stage;

        // Self
        public Guid SelfGuid;
        public string SelfPlayerName;
        public int SelfPlayerIndex;
        public int SelfBaseChunkX;
        public int SelfBaseChunkY;

        // Player list
        public readonly List<PlayerListEntry> PlayerList = new List<PlayerListEntry>();

        // Map
        public readonly Map Map = new Map();
        public readonly byte[] FogOfWar = new byte[Map.MapSize * Map.MapSize];

        // Selected entity
        public Entity SelectedEntity;
        public Entity.EntityDirection? SelectedEntityMoveDirection;

        // Scripts
        public readonly Dictionary<int, string> EntityScriptPaths = new Dictionary<int, string>();
        public readonly Dictionary<string, string> Scripts = new Dictionary<string, string>();

        // Scripting
        public readonly Dictionary<int, KeraLua.Lua> LuasByEntityId = new Dictionary<int, KeraLua.Lua>();

        // Ticking
        public int TickIndex;

        readonly Engine _engine;
        public EngineState(Engine engine) { _engine = engine; }

        public void SetName(string name)
        {
            SelfPlayerName = name;
            File.WriteAllText(_engine.SettingsFilePath, SelfPlayerName);

            Stage = EngineStage.Loading;
            _engine.Desktop.SetRootElement(_engine.LoadingView);
            _engine.Desktop.FocusedElement = null;

            _engine.PacketWriter.WriteByteLengthString(Protocol.VersionString);
            _engine.PacketWriter.WriteBytes(SelfGuid.ToByteArray());
            _engine.PacketWriter.WriteByteLengthString(SelfPlayerName);
            _engine.SendPacket();
        }

        public void SelectEntity(Entity entity)
        {
            SelectedEntity = entity;
            _engine.InGameView.OnSelectedEntityChanged();
        }

        public void SetMoveTowards(Entity.EntityDirection direction)
        {
            SelectedEntityMoveDirection = direction;
            PlanMove(SelectedEntity.GetMoveForTargetDirection(direction));
        }

        public void StopMovingTowards(Entity.EntityDirection direction)
        {
            if (SelectedEntityMoveDirection == direction) SelectedEntityMoveDirection = null;
        }

        public void PlanMove(Entity.EntityMove move)
        {
            var writer = _engine.PacketWriter;
            writer.WriteByte((byte)Protocol.ClientPacketType.PlanMoves);
            writer.WriteInt(TickIndex);
            writer.WriteShort(1);
            writer.WriteInt(SelectedEntity.Id);
            writer.WriteByte((byte)move);
            _engine.SendPacket();
        }

        public void CreateScriptForSelectedEntity()
        {
            string relativePath;
            var index = 0;
            var suffix = "";

            while (true)
            {
                relativePath = $"Script{suffix}.lua";
                if (!File.Exists(Path.Combine(_engine.ScriptsPath, relativePath))) break;
                index++;
                suffix = $"_{index}";
            }

            var defaultScriptText = "function tick(self)\n  \nend\n";
            File.WriteAllText(Path.Combine(_engine.ScriptsPath, relativePath), defaultScriptText);
            Scripts.Add(relativePath, defaultScriptText);
            _engine.InGameView.OnScriptListUpdated();

            SetupScriptPathForSelectedEntity(relativePath);
        }

        public void SetupScriptPathForSelectedEntity(string scriptFilePath)
        {
            EntityScriptPaths[SelectedEntity.Id] = scriptFilePath;
            SetupLuaForEntity(SelectedEntity.Id, scriptFilePath != null ? Scripts[scriptFilePath] : null);
            _engine.InGameView.OnSelectedEntityChanged();
        }

        public void ClearScriptPathForSelectedEntity()
        {
            EntityScriptPaths.Remove(SelectedEntity.Id);
            SetupLuaForEntity(SelectedEntity.Id, null);
            _engine.InGameView.OnSelectedEntityChanged();
        }

        public void UpdateSelectedEntityScript(string scriptText)
        {
            UpdateScriptText(EntityScriptPaths[SelectedEntity.Id], scriptText);
        }

        void UpdateScriptText(string relativePath, string scriptText)
        {
            File.WriteAllText(Path.Combine(_engine.ScriptsPath, relativePath), scriptText);
            Scripts[relativePath] = scriptText;

            foreach (var (entityId, scriptPath) in EntityScriptPaths)
            {
                if (scriptPath == relativePath) SetupLuaForEntity(entityId, scriptText);
            }
        }

        void SetupLuaForEntity(int entityId, string scriptText)
        {
            if (LuasByEntityId.Remove(entityId, out var oldLua)) oldLua.Dispose();

            if (scriptText != null)
            {
                var lua = new KeraLua.Lua(openLibs: true);
                LuasByEntityId.Add(entityId, lua);

                if (lua.DoString(scriptText))
                {
                    // TODO: Display error in UI / on entity as an icon
                    var error = lua.ToString(-1);
                    Trace.WriteLine("Error: " + error);
                }
            }
        }
    }
}
