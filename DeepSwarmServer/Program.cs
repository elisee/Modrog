using System;
using System.Diagnostics;
using System.Threading;

namespace DeepSwarmServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Guid hostGuid = Guid.Empty;
            if (args.Length != 0 && !Guid.TryParse(args[0], out hostGuid)) throw new Exception("Failed to parse argument as host Guid.");

            var serverState = new ServerState(hostGuid);
            serverState.Start();

            var cancelTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => { e.Cancel = true; cancelTokenSource.Cancel(); };

            var stopwatch = Stopwatch.StartNew();

            while (!cancelTokenSource.IsCancellationRequested)
            {
                var deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();

                serverState.Update(deltaTime);

                Thread.Sleep(1);
            }

            serverState.Stop();
            cancelTokenSource.Dispose();

            /*
            var random = new Random();

            var mapFilePath = Path.Combine(AppContext.BaseDirectory, "Map.dat");
            var map = new Map();

            if (!isNew && File.Exists(mapFilePath))
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


            Console.WriteLine($"Saving map to {mapFilePath} before quitting...");
            map.SaveToFile(mapFilePath);
            Console.WriteLine("Map saved, quitting.");

            void Tick()
            {
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
            } */
        }
    }
}
