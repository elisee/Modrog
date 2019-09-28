using DeepSwarmCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace DeepSwarmClient
{
    enum EngineView { Connect, EnterName, Loading, Playing, }

    partial class EngineState
    {
        public bool IsRunning = true;

        // View
        public EngineView View;

        // Networking
        Socket _socket;
        PacketReceiver _packetReceiver;
        public readonly PacketWriter PacketWriter = new PacketWriter();
        public readonly PacketReader PacketReader = new PacketReader();

        public string SavedServerAddress = "localhost"; // TODO: Save and laod from settings

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

            View = EngineView.EnterName;
            _engine.Interface.OnViewChanged();
        }

        void SendPacket()
        {
            try { _socket.Send(PacketWriter.Buffer, 0, PacketWriter.Finish(), SocketFlags.None); } catch { }
        }

        public void SetName(string name)
        {
            SelfPlayerName = name;
            File.WriteAllText(_engine.SettingsFilePath, SelfPlayerName);

            View = EngineView.Loading;
            _engine.Interface.OnViewChanged();

            PacketWriter.WriteByteLengthString(Protocol.VersionString);
            PacketWriter.WriteBytes(SelfGuid.ToByteArray());
            PacketWriter.WriteByteLengthString(SelfPlayerName);
            SendPacket();
        }

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
            PacketWriter.WriteByte((byte)Protocol.ClientPacketType.PlanMoves);
            PacketWriter.WriteInt(TickIndex);
            PacketWriter.WriteShort(1);
            PacketWriter.WriteInt(SelectedEntity.Id);
            PacketWriter.WriteByte((byte)move);
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
