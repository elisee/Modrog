using DeepSwarmCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace DeepSwarmServer
{
    enum ServerStage { Lobby, Playing }

    partial class ServerState
    {
        ServerStage _stage = ServerStage.Lobby;
        Guid _hostGuid = Guid.Empty;

        // Networking
        Socket _listenerSocket;

        readonly Dictionary<Socket, Peer> _peersBySocket = new Dictionary<Socket, Peer>();
        readonly List<Socket> _unindentifiedPeerSockets = new List<Socket>();
        readonly List<Socket> _identifiedPeerSockets = new List<Socket>();
        readonly List<PlayerIdentity> _playerIdentities = new List<PlayerIdentity>();

        readonly List<Socket> _pollSockets = new List<Socket>();

        readonly PacketWriter _packetWriter = new PacketWriter();
        readonly PacketReader _packetReader = new PacketReader();

        // Lobby
        readonly List<ScenarioEntry> _scenarioEntries = new List<ScenarioEntry>();
        // readonly List<SavedGameEntry> _savedGameEntries = new List<SavedGameEntry>();

        // Playing
        ScenarioEntry _activeScenario;

        float _tickAccumulatedTime = 0f;
        int _tickIndex = -1;
        const float TickInterval = 0.2f;

        public ServerState(Guid hostGuid)
        {
            _hostGuid = hostGuid;

            var scenariosPath = FileHelper.FindAppFolder("Scenarios");

            foreach (var folder in Directory.GetDirectories(scenariosPath, "*.*", SearchOption.TopDirectoryOnly))
            {
                var entry = new ScenarioEntry
                {
                    Name = folder[(scenariosPath.Length + 1)..],
                    Description = File.ReadAllText(Path.Combine(folder, "Description.txt")).Replace("\r", "")
                };

                _scenarioEntries.Add(entry);
            }
        }

        public void Start()
        {
            _listenerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                LingerState = new LingerOption(true, seconds: 1)
            };

            _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, Protocol.Port));
            _listenerSocket.Listen(64);
            Console.WriteLine($"Server listening on port {Protocol.Port}.");
        }

        public void Stop()
        {
            _listenerSocket.Close();
        }

        public void Update(float deltaTime)
        {
            _pollSockets.Clear();
            _pollSockets.Add(_listenerSocket);
            _pollSockets.AddRange(_unindentifiedPeerSockets);
            _pollSockets.AddRange(_identifiedPeerSockets);

            Socket.Select(_pollSockets, null, null, 0);

            foreach (var readSocket in _pollSockets)
            {
                if (readSocket == _listenerSocket)
                {
                    var newSocket = _listenerSocket.Accept();
                    _unindentifiedPeerSockets.Add(newSocket);
                    _peersBySocket.Add(newSocket, new Peer(newSocket));
                    Console.WriteLine($"{newSocket.RemoteEndPoint} - Socket connected, waiting for {nameof(Protocol.ClientPacketType.Hello)} packet.");
                }
                else
                {
                    ReadFromPeer(_peersBySocket[readSocket]);
                }
            }

            if (_stage == ServerStage.Playing)
            {
                _tickAccumulatedTime += deltaTime;

                if (_tickAccumulatedTime > TickInterval)
                {
                    Tick();
                    _tickAccumulatedTime %= TickInterval;
                }
            }
        }

        void ReadFromPeer(Peer peer)
        {
            if (!peer.Receiver.Read(out var packets)) { KickPeer(peer, null); return; }

            foreach (var packet in packets)
            {
                try
                {
                    _packetReader.Open(packet);
                    ReadPacket(peer);
                }
                catch (PacketException exception)
                {
                    KickPeer(peer, exception.Message);
                }
                catch (Exception exception)
                {
                    KickPeer(peer, $"Unhandled exception: {exception.Message}");
                }
            }
        }

        void KickPeer(Peer peer, string kickReason)
        {
            if (kickReason != null)
            {
                _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Kick);
                _packetWriter.WriteByteSizeString(kickReason);
                Send(peer.Socket);
            }

            var endPoint = peer.Socket.RemoteEndPoint;
            peer.Socket.Close();

            _peersBySocket.Remove(peer.Socket);

            if (peer.Identity != null)
            {
                peer.Identity.IsOnline = false;
                _identifiedPeerSockets.Remove(peer.Socket);
                _playerIdentities.Remove(peer.Identity);
                BroadcastPlayerList();
            }
            else
            {
                _unindentifiedPeerSockets.Remove(peer.Socket);
            }

            if (kickReason == null) Console.WriteLine($"{endPoint} - Socket disconnected.");
            else Console.WriteLine($"{endPoint} - Kicked: {kickReason}");
        }

        void Broadcast()
        {
            var length = _packetWriter.Finish();
            // Console.WriteLine($"Broadcasting {length} bytes");

            foreach (var socket in _identifiedPeerSockets)
            {
                try { socket.Send(_packetWriter.Buffer, 0, length, SocketFlags.None); } catch { }
            }
        }

        void Send(Socket socket)
        {
            var length = _packetWriter.Finish();
            // Console.WriteLine($"Sending {length} bytes");

            try { socket.Send(_packetWriter.Buffer, 0, length, SocketFlags.None); } catch { }
        }

        void BroadcastPlayerList()
        {
            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.PlayerList);
            _packetWriter.WriteInt(_playerIdentities.Count);

            foreach (var identity in _playerIdentities)
            {
                _packetWriter.WriteByteSizeString(identity.Name);
                _packetWriter.WriteByte((byte)((identity.IsHost ? 1 : 0) | (identity.IsOnline ? 2 : 0) | (identity.IsReady ? 4 : 0)));
            }

            Broadcast();
        }

        void Tick()
        {
            throw new NotImplementedException();

            /*
            tickIndex++;

            // Spawn new players
            var hasAnyoneChangedTeam = false;

            foreach (var player in map.PlayersInOrder)
            {
                if (player.Team != Player.PlayerTeam.None) continue;

                player.Team = (map.BluePlayerCount <= map.RedPlayerCount) ? Player.PlayerTeam.Blue : Player.PlayerTeam.Red;
                if (player.Team == Player.PlayerTeam.Blue) map.BluePlayerCount++;
                else map.RedPlayerCount++;
                hasAnyoneChangedTeam = true;

                var index = random.Next(map.FreeChunkIndices.Count);
                var chunkIndex = map.FreeChunkIndices[index];
                map.FreeChunkIndices.RemoveAt(index);

                player.BaseChunkX = chunkIndex % Map.ChunkCount;
                player.BaseChunkY = chunkIndex / Map.ChunkCount;

                var centerX = player.BaseChunkX * Map.ChunkSize + Map.ChunkSize / 2;
                var centerY = player.BaseChunkY * Map.ChunkSize + Map.ChunkSize / 2;

                map.PokeCircle(centerX, centerY, Map.Tile.Path, 6);
                map.MakeEntity(Entity.EntityType.Factory, player.PlayerIndex, centerX, centerY - 2, Entity.EntityDirection.Down);
                map.MakeEntity(Entity.EntityType.Heart, player.PlayerIndex, centerX, centerY - 3, Entity.EntityDirection.Up);
            }

            map.AddUpcomingEntities();

            if (hasAnyoneChangedTeam) BroadcastPlayerList();

            // Apply planned moves
            // TODO: Decide movement priority, maybe based on robot type or time the moves were received
            foreach (var entity in map.Entities)
            {
                switch (entity.UpcomingMove)
                {
                    case Entity.EntityMove.RotateCW:
                        if (entity.Type == Entity.EntityType.Robot)
                        {
                            entity.Direction = (Entity.EntityDirection)((int)(entity.Direction + 1) % 4);
                        }
                        break;
                    case Entity.EntityMove.RotateCCW:
                        if (entity.Type == Entity.EntityType.Robot)
                        {
                            entity.Direction = (Entity.EntityDirection)((int)(entity.Direction + 3) % 4);
                        }
                        break;

                    case Entity.EntityMove.Forward:
                        if (entity.Type == Entity.EntityType.Robot)
                        {
                            var newX = entity.X;
                            var newY = entity.Y;

                            switch (entity.Direction)
                            {
                                case Entity.EntityDirection.Right: newX++; break;
                                case Entity.EntityDirection.Down: newY++; break;
                                case Entity.EntityDirection.Left: newX--; break;
                                case Entity.EntityDirection.Up: newY--; break;
                            }

                            var targetTile = map.PeekTile(newX, newY);

                            switch (targetTile)
                            {
                                case Map.Tile.Rock:
                                    // Can't move
                                    break;

                                case Map.Tile.Path:
                                    entity.X = Map.Wrap(newX);
                                    entity.Y = Map.Wrap(newY);

                                    foreach (var otherEntity in map.Entities)
                                    {
                                        if (otherEntity == entity || otherEntity.Type != Entity.EntityType.Factory) continue;
                                        if (entity.X != otherEntity.X || entity.Y != otherEntity.Y) continue;

                                        var entityTeam = map.PlayersInOrder[entity.PlayerIndex].Team;
                                        var factoryTeam = map.PlayersInOrder[otherEntity.PlayerIndex].Team;

                                        if (entityTeam == factoryTeam)
                                        {
                                            otherEntity.Crystals += entity.Crystals;
                                            entity.Crystals = 0;

                                            break;
                                        }
                                    }
                                    break;

                                case Map.Tile.Dirt1:
                                case Map.Tile.Dirt2:
                                    map.PokeTile(newX, newY, targetTile + 1);
                                    break;
                                case Map.Tile.Dirt3:
                                    map.PokeTile(newX, newY, Map.Tile.Path);
                                    break;

                                case Map.Tile.Crystal1:
                                case Map.Tile.Crystal2:
                                case Map.Tile.Crystal3:
                                case Map.Tile.Crystal4:
                                    map.PokeTile(newX, newY, targetTile + 1);
                                    break;
                                case Map.Tile.Crystal5:
                                    map.PokeTile(newX, newY, Map.Tile.Path);
                                    entity.Crystals += 1;
                                    break;
                            }
                        }
                        break;

                    case Entity.EntityMove.Attack:
                        if (entity.Type == Entity.EntityType.Robot)
                        {
                        }
                        break;

                    case Entity.EntityMove.Build:
                        if (entity.Type == Entity.EntityType.Factory)
                        {
                            var buildPrice = Entity.EntityStatsByType[(int)Entity.EntityType.Robot].BuildPrice;
                            if (entity.Crystals >= buildPrice)
                            {
                                entity.Crystals -= buildPrice;
                                map.MakeEntity(Entity.EntityType.Robot, entity.PlayerIndex, entity.X, entity.Y + 1, Entity.EntityDirection.Down);
                            }
                        }
                        break;

                    case Entity.EntityMove.Idle:
                        break;
                }

                entity.UpcomingMove = Entity.EntityMove.Idle;
            }

            map.AddUpcomingEntities();

            // TODO: Resolve damage

            // Gather new data to send to players
            foreach (var peer in playingPeers)
            {
                var player = peer.Player;
                var seenEntities = new HashSet<Entity>();
                var seenTiles = new Dictionary<(int, int), Map.Tile>();

                foreach (var ownedEntity in player.OwnedEntities)
                {
                    var stats = Entity.EntityStatsByType[(int)ownedEntity.Type];

                    var squaredOmniViewRadius = stats.OmniViewRadius * stats.OmniViewRadius;
                    var squaredDirectionalViewRadius = stats.DirectionalViewRadius * stats.DirectionalViewRadius;
                    var directionAngle = ownedEntity.GetDirectionAngle();

                    var radius = Math.Max(stats.OmniViewRadius, stats.DirectionalViewRadius);
                    seenTiles.TryAdd((ownedEntity.X, ownedEntity.Y), map.PeekTile(ownedEntity.X, ownedEntity.Y));

                    for (var dy = -radius; dy <= radius; dy++)
                    {
                        for (var dx = -radius; dx <= radius; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            var angle = MathF.Atan2(dy, dx);
                            var squaredDistance = dx * dx + dy * dy;

                            var isInView =
                                squaredDistance <= squaredOmniViewRadius ||
                                (squaredDistance <= squaredDirectionalViewRadius &&
                                Math.Abs(MathHelper.WrapAngle(angle - directionAngle)) < stats.HalfFieldOfView);

                            if (!isInView) continue;

                            var targetX = ownedEntity.X + dx;
                            var targetY = ownedEntity.Y + dy;

                            var hasLineOfSight =
                                map.HasLineOfSight(ownedEntity.X, ownedEntity.Y, targetX, targetY) ||
                                map.HasLineOfSight(ownedEntity.X, ownedEntity.Y, targetX - Math.Sign(dx), targetY) ||
                                map.HasLineOfSight(ownedEntity.X, ownedEntity.Y, targetX, targetY - Math.Sign(dy));
                            if (!hasLineOfSight) continue;

                            targetX = Map.Wrap(targetX);
                            targetY = Map.Wrap(targetY);

                            seenTiles.TryAdd((targetX, targetY), map.PeekTile(targetX, targetY));
                            var targetEntity = map.PeekEntity(targetX, targetY);
                            if (targetEntity != null && targetEntity.PlayerIndex != player.PlayerIndex) seenEntities.Add(targetEntity);
                        }
                    }
                }

                // Send seen state to player
                writer.WriteByte((byte)Protocol.ServerPacketType.Tick);
                writer.WriteInt(tickIndex);

                writer.WriteShort((short)(player.OwnedEntities.Count + seenEntities.Count));

                foreach (var entity in player.OwnedEntities)
                {
                    writer.WriteInt(entity.Id);
                    writer.WriteShort((short)entity.X);
                    writer.WriteShort((short)entity.Y);
                    writer.WriteShort((byte)entity.PlayerIndex);
                    writer.WriteByte((byte)entity.Type);
                    writer.WriteByte((byte)entity.Direction);
                    writer.WriteByte((byte)entity.Health);
                    writer.WriteInt(entity.Crystals);
                }

                foreach (var entity in seenEntities)
                {
                    writer.WriteShort((short)entity.X);
                    writer.WriteShort((short)entity.Y);
                    writer.WriteShort((byte)entity.PlayerIndex);
                    writer.WriteByte((byte)entity.Type);
                    writer.WriteByte((byte)entity.Direction);
                    writer.WriteByte((byte)entity.Health);
                    writer.WriteInt(entity.Crystals);
                }

                writer.WriteShort((short)seenTiles.Count);
                foreach (var ((x, y), tile) in seenTiles)
                {
                    writer.WriteShort((short)x);
                    writer.WriteShort((short)y);
                    writer.WriteByte((byte)tile);
                }

                Send(peer.Socket);
            }
            */
        }
    }
}
