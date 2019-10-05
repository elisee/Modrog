using DeepSwarmCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static DeepSwarmCommon.Protocol;


namespace DeepSwarmClient
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

                        case ServerPacketType.PlayerList: ReadPlayerList(); break;
                        case ServerPacketType.Chat: ReadChat(); break;

                        case ServerPacketType.SetupGame:
                            if (!EnsureView(EngineView.Lobby)) break;
                            ReadSetupGame();
                            break;

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

        #region Loading View
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

                ReadSetupGame();
            }
            else
            {

            }

            _engine.Interface.OnViewChanged();
        }
        #endregion

        #region Lobby or Playing View
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
            var author = _packetReader.ReadByteSizeString();
            var message = _packetReader.ReadByteSizeString();

            switch (View)
            {
                case EngineView.Playing: _engine.Interface.PlayingView.OnChatMessageReceived(author, message); break;
                case EngineView.Lobby: _engine.Interface.LobbyView.OnChatMessageReceived(author, message); break;
            }
        }
        #endregion

        #region Lobby View
        void ReadSetupGame()
        {
            var scenarioName = _packetReader.ReadByteSizeString();
            ActiveScenario = ScenarioEntries.Find(x => x.Name == scenarioName);
            _engine.Interface.LobbyView.OnActiveScenarioChanged();
        }
        #endregion

        #region Playing View
        void ReadTick()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
