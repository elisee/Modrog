using DeepSwarmClient.UI;
using DeepSwarmCommon;
using System.IO;

namespace DeepSwarmClient
{
    partial class Engine
    {
        public void FromUI_SetName(string name)
        {
            SelfState.PlayerName = name;
            File.WriteAllText(SettingsFilePath, SelfState.PlayerName);

            ActiveStage = EngineStage.Loading;
            Desktop.SetRootElement(LoadingView);
            Desktop.FocusedElement = null;

            _writer.WriteByteLengthString(Protocol.VersionString);
            _writer.WriteBytes(SelfState.Guid.ToByteArray());
            _writer.WriteByteLengthString(SelfState.PlayerName);

            SendPacket();
        }

        public void FromUI_SelectEntity(Entity entity)
        {
            SelectedEntity = entity;
            InGameView.OnSelectedEntityChanged();
        }

        public void FromUI_SetMoveTowards(Entity.EntityDirection direction)
        {
            _selectedEntityMoveDirection = direction;
            FromUI_PlanMove(SelectedEntity.GetMoveForTargetDirection(direction));
        }

        public void FromUI_StopMovingTowards(Entity.EntityDirection direction)
        {
            if (_selectedEntityMoveDirection == direction) _selectedEntityMoveDirection = null;
        }

        public void FromUI_PlanMove(Entity.EntityMove move)
        {
            _writer.WriteByte((byte)Protocol.ClientPacketType.PlanMoves);
            _writer.WriteInt(_tickIndex);
            _writer.WriteShort(1);
            _writer.WriteInt(SelectedEntity.Id);
            _writer.WriteByte((byte)move);
            SendPacket();
        }

        public void FromUI_CreateScriptForSelectedEntity()
        {
            string relativePath;
            var index = 0;
            var suffix = "";

            while (true)
            {
                relativePath = $"Script{suffix}.lua";
                if (!File.Exists(Path.Combine(ScriptsPath, relativePath))) break;
                index++;
                suffix = $"_{index}";
            }

            var defaultScriptText = "function tick(self)\n  \nend\n";
            File.WriteAllText(Path.Combine(ScriptsPath, relativePath), defaultScriptText);
            Scripts.Add(relativePath, defaultScriptText);
            InGameView.OnScriptListUpdated();

            FromUI_SetupScriptPathForSelectedEntity(relativePath);
        }

        public void FromUI_SetupScriptPathForSelectedEntity(string scriptFilePath)
        {
            EntityScriptPaths[SelectedEntity.Id] = scriptFilePath;
            SetupLuaForEntity(SelectedEntity.Id, scriptFilePath != null ? Scripts[scriptFilePath] : null);
            InGameView.OnSelectedEntityChanged();
        }

        public void FromUI_ClearScriptPathForSelectedEntity()
        {
            EntityScriptPaths.Remove(SelectedEntity.Id);
            SetupLuaForEntity(SelectedEntity.Id, null);
            InGameView.OnSelectedEntityChanged();
        }

        public void FromUI_UpdateSelectedEntityScript(string scriptText)
        {
            UpdateScriptText(EntityScriptPaths[SelectedEntity.Id], scriptText);
        }
    }
}
