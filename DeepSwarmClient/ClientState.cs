using DeepSwarmCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace DeepSwarmClient
{
    enum EngineView { Connect, Loading, Lobby, Playing, Disconnected }

    partial class ClientState
    {
        public bool IsRunning = true;

        // View
        public EngineView View;

        // Networking
        Socket _socket;
        PacketReceiver _packetReceiver;
        readonly PacketWriter _packetWriter = new PacketWriter();
        readonly PacketReader _packetReader = new PacketReader();

        public string SavedServerAddress = "localhost"; // TODO: Save and load from settings

        // Self
        public Guid SelfGuid;
        public string SelfPlayerName;
        public int SelfPlayerIndex;
        public int SelfBaseChunkX;
        public int SelfBaseChunkY;

        // Player list
        public readonly List<PlayerListEntry> PlayerList = new List<PlayerListEntry>();

        // Lobby
        public readonly List<ScenarioEntry> ScenarioEntries = new List<ScenarioEntry>();
        // public readonly List<SavedGameEntry> SavedGameEntries = new List<SavedGameEntry>();
        public ScenarioEntry ActiveScenario;

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

        public ClientState(Engine engine) { _engine = engine; }

        public void Stop()
        {
            _socket?.Close();
            _socket = null;

            IsRunning = false;
        }

        public void Connect(string address)
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true, LingerState = new LingerOption(true, seconds: 1) };

            // TODO: Do this in a background thread and have a connecting popup
            try
            {
                _socket.Connect(new IPEndPoint(IPAddress.Loopback, Protocol.Port));
            }
            catch
            {
                return;
            }

            _packetReceiver = new PacketReceiver(_socket);

            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.Hello);
            _packetWriter.WriteByteSizeString(Protocol.VersionString);
            _packetWriter.WriteBytes(SelfGuid.ToByteArray());
            _packetWriter.WriteByteSizeString(SelfPlayerName);
            SendPacket();

            View = EngineView.Loading;
            _engine.Interface.OnViewChanged();
        }

        public void Disconnect()
        {
            _socket?.Close();
            _socket = null;

            View = EngineView.Connect;
            _engine.Interface.OnViewChanged();
        }

        void SendPacket()
        {
            try { _socket.Send(_packetWriter.Buffer, 0, _packetWriter.Finish(), SocketFlags.None); } catch { }
        }

        #region Connect View
        public void SetName(string name)
        {
            SelfPlayerName = name;
            File.WriteAllText(_engine.SettingsFilePath, SelfPlayerName);
        }
        #endregion

        #region Lobby View
        public void ChooseScenario(string scenarioName)
        {
            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.ChooseGame);
            // TODO: Add a byte to say whether scenario or saved game
            _packetWriter.WriteByteSizeString(scenarioName);
            SendPacket();
        }
        #endregion

        #region Playing View
        public void SelectEntity(Entity entity)
        {
            SelectedEntity = entity;
            _engine.Interface.PlayingView.OnSelectedEntityChanged();
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
            _packetWriter.WriteByte((byte)Protocol.ClientPacketType.PlanMoves);
            _packetWriter.WriteInt(TickIndex);
            _packetWriter.WriteShort(1);
            _packetWriter.WriteInt(SelectedEntity.Id);
            _packetWriter.WriteByte((byte)move);
            SendPacket();
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
            _engine.Interface.PlayingView.OnScriptListUpdated();

            SetupScriptPathForSelectedEntity(relativePath);
        }

        public void SetupScriptPathForSelectedEntity(string scriptFilePath)
        {
            EntityScriptPaths[SelectedEntity.Id] = scriptFilePath;
            SetupLuaForEntity(SelectedEntity.Id, scriptFilePath != null ? Scripts[scriptFilePath] : null);
            _engine.Interface.PlayingView.OnSelectedEntityChanged();
        }

        public void ClearScriptPathForSelectedEntity()
        {
            EntityScriptPaths.Remove(SelectedEntity.Id);
            SetupLuaForEntity(SelectedEntity.Id, null);
            _engine.Interface.PlayingView.OnSelectedEntityChanged();
        }

        public void UpdateSelectedEntityScript(string scriptText)
        {
            UpdateScriptText(EntityScriptPaths[SelectedEntity.Id], scriptText);
        }

        public void RenameSelectedEntityScript(string newRelativePath)
        {
            if (Scripts.ContainsKey(newRelativePath)) throw new Exception($"There is already a script with this path: {newRelativePath}");
            if (newRelativePath.Contains("..")) throw new Exception($"Invalid new path for script: {newRelativePath}");

            var oldRelativePath = EntityScriptPaths[SelectedEntity.Id];
            var oldPath = Path.Combine(_engine.ScriptsPath, oldRelativePath);
            var newPath = Path.Combine(_engine.ScriptsPath, newRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Move(oldPath, newPath);

            Scripts.Remove(oldRelativePath, out var scriptText);
            Scripts.Add(newRelativePath, scriptText);

            var entitiesToUpdate = new List<int>();

            foreach (var (entityId, path) in EntityScriptPaths)
            {
                if (path == oldRelativePath) entitiesToUpdate.Add(entityId);
            }

            foreach (var entityId in entitiesToUpdate) EntityScriptPaths[entityId] = newRelativePath;

            _engine.Interface.PlayingView.OnScriptListUpdated();
            _engine.Interface.PlayingView.OnSelectedEntityChanged();
        }
        #endregion

        internal void Update(float deltaTime)
        {
            if (_socket != null && _socket.Poll(0, SelectMode.SelectRead))
            {
                if (!_packetReceiver.Read(out var packets))
                {
                    Trace.WriteLine($"Disconnected from server.");
                    Stop();
                    return;
                }

                ReadPackets(packets);
            }
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
