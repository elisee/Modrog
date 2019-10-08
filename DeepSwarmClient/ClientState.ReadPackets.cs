using DeepSwarmApi;
using DeepSwarmBasics.Math;
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
                Trace.WriteLine($"Aborted: {reason}");
                Disconnect(error: $"Aborted: {reason}");
            }

            foreach (var packet in packets)
            {
                _packetReader.Open(packet);
                if (Stage == ClientStage.Exited) break;

                try
                {
                    var packetType = (Protocol.ServerPacketType)_packetReader.ReadByte();

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

                        case ServerPacketType.SetPeerOnline:
                            if (!EnsureStage(ClientStage.Playing)) break;
                            throw new NotImplementedException();
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

        #region Loading Stage
        void ReadWelcome()
        {
            var isPlaying = _packetReader.ReadByte() != 0;
            Stage = isPlaying ? ClientStage.Playing : ClientStage.Lobby;

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

                ReadSetScenario();
            }
            else
            {
                WorldSize = Point.Zero;
                WorldTiles = new short[0];
                WorldFog = new byte[0];
                SeenEntities.Clear();
            }

            _engine.Interface.OnStageChanged();
        }
        #endregion

        #region Lobby or Playing Stage
        void ReadChat()
        {
            var author = _packetReader.ReadByteSizeString();
            var message = _packetReader.ReadByteSizeString();

            switch (Stage)
            {
                case ClientStage.Lobby: _engine.Interface.LobbyView.OnChatMessageReceived(author, message); break;
                case ClientStage.Playing: _engine.Interface.PlayingView.OnChatMessageReceived(author, message); break;
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
                case ClientStage.Lobby: _engine.Interface.LobbyView.OnPlayerListUpdated(); break;
                case ClientStage.Playing: _engine.Interface.PlayingView.OnPlayerListUpdated(); break;
            }
        }

        void ReadSetScenario()
        {
            var scenarioName = _packetReader.ReadByteSizeString();
            ActiveScenario = ScenarioEntries.Find(x => x.Name == scenarioName);
            _engine.Interface.LobbyView.OnActiveScenarioChanged();
        }

        void ReadSetupCountdown()
        {
            IsCountingDown = _packetReader.ReadByte() != 0;
            _engine.Interface.LobbyView.OnIsCountingDownChanged();
        }
        #endregion

        #region Playing Stage
        void ReadTick()
        {
            if (Stage == ClientStage.Lobby)
            {
                Stage = ClientStage.Playing;
                _engine.Interface.OnStageChanged();
            }

            TickIndex = _packetReader.ReadInt();
            SeenEntities.Clear();

            var wasTeleported = _packetReader.ReadByte() != 0;
            if (wasTeleported)
            {
                WorldSize = new Point(_packetReader.ReadShort(), _packetReader.ReadShort());
                WorldTiles = new short[WorldSize.X * WorldSize.Y];
                WorldFog = new byte[WorldSize.X * WorldSize.Y];

                var location = new Point(_packetReader.ReadShort(), _packetReader.ReadShort());
                _engine.Interface.PlayingView.OnTeleported(location);
            }

            if (WorldTiles == null) throw new Exception("Server didn't teleport client on first tick.");

            Unsafe.InitBlock(ref WorldFog[0], 0, (uint)WorldFog.Length);

            var entitiesCount = (int)_packetReader.ReadShort();
            for (var i = 0; i < entitiesCount; i++)
            {
                var id = _packetReader.ReadInt();
                var x = _packetReader.ReadShort();
                var y = _packetReader.ReadShort();
                var direction = (EntityDirection)_packetReader.ReadByte();
                var playerIndex = _packetReader.ReadShort();

                SeenEntities.Add(new Game.ClientEntity(id) { Position = new Point(x, y), Direction = direction, PlayerIndex = playerIndex });
            }

            var tilesCount = (int)_packetReader.ReadShort();
            for (var i = 0; i < tilesCount; i++)
            {
                var x = _packetReader.ReadShort();
                var y = _packetReader.ReadShort();
                var tile = _packetReader.ReadShort();
                WorldTiles[y * WorldSize.X + x] = tile;
                WorldFog[y * WorldSize.X + x] = 1;
            }

            // Send update
            var position = new Point(
                (int)(_engine.Interface.PlayingView.ScrollingPixelsX / Interface.Playing.PlayingView.TileSize),
                (int)(_engine.Interface.PlayingView.ScrollingPixelsY / Interface.Playing.PlayingView.TileSize));

            _packetWriter.WriteByte((byte)ClientPacketType.SetPosition);
            _packetWriter.WriteShort((short)position.X);
            _packetWriter.WriteShort((short)position.Y);
            SendPacket();
        }
        #endregion
    }
}
