using DeepSwarmCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DeepSwarmServer
{
    class Program
    {
        static void Main()
        {
            var random = new Random();

            var mapFilePath = Path.Combine(AppContext.BaseDirectory, "Map.dat");
            var map = new Map();

            if (File.Exists(mapFilePath))
            {
                Console.WriteLine($"Loading map from {mapFilePath}...");
                map.LoadFromFile(mapFilePath);
                Console.WriteLine($"Done loading map.");
            }
            else
            {
                Console.WriteLine($"Generating map, saving to {mapFilePath}...");
                map.Generate();
                map.SaveToFile(mapFilePath);
                Console.WriteLine($"Done generating map.");
            }

            var listenerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                LingerState = new LingerOption(true, seconds: 1)
            };

            listenerSocket.Bind(new IPEndPoint(IPAddress.Any, Protocol.Port));
            listenerSocket.Listen(64);
            Console.WriteLine($"Server listening on port {Protocol.Port}.");

            var peerSockets = new List<Socket>();
            var peersBySocket = new Dictionary<Socket, Peer>();
            var peersByGuid = new Dictionary<Guid, Peer>();
            var playingPeers = new List<Peer>();

            var pollSockets = new List<Socket>();
            var writer = new PacketWriter();
            var reader = new PacketReader();

            var cancelTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => { e.Cancel = true; cancelTokenSource.Cancel(); };

            var stopwatch = Stopwatch.StartNew();
            var accumulatedTime = 0f;
            const float TickInterval = 1000f / 2f;

            while (!cancelTokenSource.IsCancellationRequested)
            {
                pollSockets.Clear();
                pollSockets.Add(listenerSocket);
                pollSockets.AddRange(peerSockets);

                Socket.Select(pollSockets, null, null, 0);

                foreach (var readSocket in pollSockets)
                {
                    if (readSocket == listenerSocket)
                    {
                        var newSocket = listenerSocket.Accept();
                        peerSockets.Add(newSocket);
                        peersBySocket.Add(newSocket, new Peer { Socket = newSocket });
                        Console.WriteLine($"{newSocket.RemoteEndPoint} - Socket connected.");
                    }
                    else
                    {
                        Read(readSocket);
                    }
                }

                var deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
                accumulatedTime += deltaTime;

                if (accumulatedTime > TickInterval)
                {
                    Tick();
                    accumulatedTime %= TickInterval;
                }

                Thread.Sleep(1);
            }

            Console.WriteLine($"Saving map to {mapFilePath} before quitting...");
            map.SaveToFile(mapFilePath);
            Console.WriteLine("Map saved, quitting.");

            void Read(Socket socket)
            {
                var peer = peersBySocket[socket];

                int bytesRead = 0;
                try { bytesRead = socket.Receive(reader.Buffer); } catch (SocketException) { }
                if (bytesRead == 0) { KickPeer(null); return; }

                void KickPeer(string kickReason)
                {
                    var endPoint = socket.RemoteEndPoint;
                    socket.Close();
                    peerSockets.Remove(socket);
                    peersBySocket.Remove(socket);
                    if (peer.Player != null) peersByGuid.Remove(peer.Player.Guid);
                    if (peer.Stage == Peer.PeerStage.Playing)
                    {
                        playingPeers.Remove(peer);
                        BroadcastPlayerList();
                    }

                    if (kickReason == null) Console.WriteLine($"{endPoint} - Socket disconnected.");
                    else Console.WriteLine($"{endPoint} - Kicked: {kickReason}");
                }

                reader.ResetCursor();

                switch (peer.Stage)
                {
                    case Peer.PeerStage.WaitingForHandshake:
                        string versionString;
                        try { versionString = reader.ReadByteSizeString(); }
                        catch { KickPeer($"Invalid {nameof(Peer.PeerStage.WaitingForHandshake)} packet."); return; }
                        if (versionString != Protocol.VersionString) { KickPeer($"Invalid protocol string, expected {Protocol.VersionString}, got {versionString}."); return; }

                        Guid playerGuid;
                        try { playerGuid = new Guid(reader.ReadBytes(16)); if (playerGuid == Guid.Empty) throw new Exception(); }
                        catch { KickPeer($"Received invalid player Guid."); return; }

                        string peerName;
                        try { peerName = reader.ReadByteSizeString(); }
                        catch { KickPeer($"Invalid {nameof(Peer.PeerStage.WaitingForHandshake)} packet."); return; }
                        if (peerName.Length == 0 || peerName.Length > Protocol.MaxPlayerNameLength) { KickPeer($"Invalid player name: {peerName}."); return; }

                        if (!peersByGuid.TryAdd(playerGuid, peer)) { KickPeer($"There is already someone connected with that Guid."); return; }

                        if (!map.PlayersByGuid.TryGetValue(playerGuid, out peer.Player))
                        {
                            peer.Player = new Player { Guid = playerGuid, PlayerIndex = map.PlayersInOrder.Count };
                            map.PlayersInOrder.Add(peer.Player);
                            map.PlayersByGuid.Add(playerGuid, peer.Player);
                        }

                        peer.Player.Name = peerName;
                        Console.WriteLine($"{socket.RemoteEndPoint} - Player name set to: " + peer.Player.Name);

                        writer.ResetCursor();
                        writer.WriteByte((byte)Protocol.ServerPacketType.SetupPlayerIndex);
                        writer.WriteInt(peer.Player.PlayerIndex);
                        Send(socket);

                        peer.Stage = Peer.PeerStage.Playing;
                        playingPeers.Add(peer);

                        BroadcastPlayerList();
                        break;

                    case Peer.PeerStage.Playing:
                        // TODO: Read upcoming moves packet or chat
                        break;
                }
            }

            void Broadcast()
            {
                // Console.WriteLine($"Broadcasting {writer.Cursor} bytes");

                foreach (var peer in playingPeers)
                {
                    try { peer.Socket.Send(writer.Buffer, 0, writer.Cursor, SocketFlags.None); } catch { }
                }
            }

            void Send(Socket socket)
            {
                // Console.WriteLine($"Sending {writer.Cursor} bytes");
                try { socket.Send(writer.Buffer, 0, writer.Cursor, SocketFlags.None); } catch { }
            }

            /* void WriteMapArea(int x, int y, int width, int height)
            {
                writer.WriteShort((short)x);
                writer.WriteShort((short)y);
                writer.WriteShort((short)width);
                writer.WriteShort((short)height);

                var area = new byte[width * height];
                for (var j = 0; j < height; j++) Buffer.BlockCopy(map.Tiles, (y + j) * Map.MapSize + x, area, j * width, width);

                writer.WriteBytes(area);

                // TODO: Send robots too
            } */

            void BroadcastPlayerList()
            {
                writer.ResetCursor();
                writer.WriteByte((byte)Protocol.ServerPacketType.PlayerList);
                writer.WriteInt(map.PlayersInOrder.Count);

                foreach (var player in map.PlayersInOrder)
                {
                    writer.WriteByteLengthString(player.Name);
                    writer.WriteByte((byte)player.Team);

                    var isOnline = peersByGuid.ContainsKey(player.Guid);
                    writer.WriteByte(isOnline ? (byte)1 : (byte)0);
                }

                Broadcast();
            }

            void Tick()
            {
                // Spawn new players
                foreach (var player in map.PlayersInOrder)
                {
                    if (player.Team != Player.PlayerTeam.None) continue;

                    player.Team = (map.BluePlayerCount <= map.RedPlayerCount) ? Player.PlayerTeam.Blue : Player.PlayerTeam.Red;
                    if (player.Team == Player.PlayerTeam.Blue) map.BluePlayerCount++;
                    else map.RedPlayerCount++;

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

                // Apply planned moves
                // TODO: Decide movement priority, maybe based on robot type or time the moves were received
                foreach (var entity in map.Entities)
                {
                    switch (entity.UpcomingMove)
                    {
                        case Entity.EntityMove.RotateCW: entity.Direction = (Entity.EntityDirection)((int)(entity.Direction + 1) % 4); break;
                        case Entity.EntityMove.RotateCCW: entity.Direction = (Entity.EntityDirection)((int)(entity.Direction + 3) % 4); break;

                        case Entity.EntityMove.Move:
                            switch (entity.Direction)
                            {
                                case Entity.EntityDirection.Right: entity.X++; break;
                                case Entity.EntityDirection.Down: entity.Y++; break;
                                case Entity.EntityDirection.Left: entity.X--; break;
                                case Entity.EntityDirection.Up: entity.Y--; break;
                            }
                            break;

                        case Entity.EntityMove.Attack:
                            break;

                        case Entity.EntityMove.Build:
                            break;

                        case Entity.EntityMove.Idle:
                            break;
                    }

                    entity.UpcomingMove = Entity.EntityMove.Idle;
                }

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

                                var hasLineOfSight = map.HasLineOfSight(ownedEntity.X, ownedEntity.Y, targetX, targetY);
                                if (!hasLineOfSight) continue;

                                seenTiles.TryAdd((targetX, targetY), map.PeekTile(targetX, targetY));
                                var targetEntity = map.PeekEntity(targetX, targetY);
                                if (targetEntity != null && targetEntity.PlayerIndex != player.PlayerIndex) seenEntities.Add(targetEntity);
                            }
                        }
                    }

                    // TODO: Send seen tiles & seen entities
                    writer.ResetCursor();
                    writer.WriteByte((byte)Protocol.ServerPacketType.Tick);

                    writer.WriteShort((short)(player.OwnedEntities.Count + seenEntities.Count));

                    foreach (var entity in player.OwnedEntities)
                    {
                        writer.WriteShort((short)entity.X);
                        writer.WriteShort((short)entity.Y);
                        writer.WriteShort((byte)entity.PlayerIndex);
                        writer.WriteByte((byte)entity.Type);
                        writer.WriteByte((byte)entity.Direction);
                        writer.WriteByte((byte)entity.Health);
                    }

                    foreach (var entity in seenEntities)
                    {
                        writer.WriteShort((short)entity.X);
                        writer.WriteShort((short)entity.Y);
                        writer.WriteShort((byte)entity.PlayerIndex);
                        writer.WriteByte((byte)entity.Type);
                        writer.WriteByte((byte)entity.Direction);
                        writer.WriteByte((byte)entity.Health);
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
            }
        }
    }
}
