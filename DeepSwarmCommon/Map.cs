using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace DeepSwarmCommon
{
    public class Map
    {
        public const int ChunkSize = 15;
        public const int ChunkCount = 50;
        public const int MapSize = ChunkSize * ChunkCount;
        public const int TileSize = 24;

        public enum Tile : byte
        {
            Unknown = 0,
            Rock = 1,
            Path = 2,
            Crystal = 3
        }

        public static readonly uint[] TileColors = new uint[] {
            0x000000ff,
            0x51260aff,
            0x271104ff,
            0xa8d618ff,
        };

        [Flags]
        enum ConnectionFlags : byte
        {
            NotUsed = 0,
            Used = 1,
            Left = 2,
            Right = 4,
            Above = 8,
            Below = 16
        }

        public readonly byte[] Tiles = new byte[MapSize * MapSize];
        public readonly List<int> FreeChunkIndices = new List<int>();

        public readonly Dictionary<Guid, Player> PlayersByGuid = new Dictionary<Guid, Player>();
        public readonly List<Player> PlayersInOrder = new List<Player>();

        public readonly List<Entity> Entities = new List<Entity>();
        public readonly Dictionary<int, Entity> EntitiesById = new Dictionary<int, Entity>();
        public int NextEntityId;

        public int BluePlayerCount = 0;
        public int RedPlayerCount = 0;

        public void LoadFromFile(string path)
        {
            using var file = File.OpenRead(path);
            using var mapReader = new BinaryReader(file);

            mapReader.Read(Tiles, 0, Tiles.Length);
            var freeChunksCount = mapReader.ReadInt32();
            for (var i = 0; i < freeChunksCount; i++) FreeChunkIndices.Add(mapReader.ReadInt32());

            var playerCount = mapReader.ReadInt32();
            for (var i = 0; i < playerCount; i++)
            {
                var player = new Player
                {
                    Guid = new Guid(mapReader.ReadBytes(16)),
                    Name = mapReader.ReadString(),
                    Team = (Player.PlayerTeam)mapReader.ReadByte(),
                    BaseChunkX = mapReader.ReadInt16(),
                    BaseChunkY = mapReader.ReadInt16(),
                    PlayerIndex = PlayersInOrder.Count
                };

                if (player.Team == Player.PlayerTeam.Blue) BluePlayerCount++;
                else if (player.Team == Player.PlayerTeam.Red) RedPlayerCount++;

                PlayersInOrder.Add(player);
                PlayersByGuid.Add(player.Guid, player);
            }

            var entitiesCount = mapReader.ReadInt32();
            NextEntityId = mapReader.ReadInt32();
            for (var i = 0; i < entitiesCount; i++)
            {
                var x = mapReader.ReadInt16();
                var y = mapReader.ReadInt16();
                var playerIndex = (int)mapReader.ReadByte();
                var type = (Entity.EntityType)mapReader.ReadByte();
                var direction = (Entity.EntityDirection)mapReader.ReadByte();
                var entity = MakeEntity(type, playerIndex, x, y, direction);

                entity.Health = mapReader.ReadByte();

            }
        }

        public void SaveToFile(string path)
        {
            using var file = File.OpenWrite(path);
            using var mapWriter = new BinaryWriter(file);

            mapWriter.Write(Tiles, 0, Tiles.Length);
            mapWriter.Write(FreeChunkIndices.Count);
            foreach (var freeChunkIndex in FreeChunkIndices) mapWriter.Write(freeChunkIndex);

            mapWriter.Write(PlayersByGuid.Count);
            foreach (var player in PlayersInOrder)
            {
                mapWriter.Write(player.Guid.ToByteArray());
                mapWriter.Write(player.Name);
                mapWriter.Write((byte)player.Team);
                mapWriter.Write((short)player.BaseChunkX);
                mapWriter.Write((short)player.BaseChunkY);
            }

            mapWriter.Write(Entities.Count);
            mapWriter.Write(NextEntityId);
            foreach (var entity in Entities)
            {
                mapWriter.Write((short)entity.X);
                mapWriter.Write((short)entity.Y);
                mapWriter.Write((byte)entity.PlayerIndex);
                mapWriter.Write((byte)entity.Type);
                mapWriter.Write((byte)entity.Direction);
                mapWriter.Write((byte)entity.Health);
            }
        }

        public void Generate()
        {
            Unsafe.InitBlock(ref Tiles[0], (byte)Tile.Rock, (uint)Tiles.Length);
            FreeChunkIndices.Clear();

            var random = new Random();
            var chunkConnections = new ConnectionFlags[ChunkCount * ChunkCount];
            var chunkPathSizes = new byte[ChunkCount * ChunkCount];
            var chunkPathOffXs = new sbyte[ChunkCount * ChunkCount];
            var chunkPathOffYs = new sbyte[ChunkCount * ChunkCount];

            for (var i = 0; i < chunkConnections.Length; i++)
            {
                chunkPathSizes[i] = (byte)(2 + random.Next(6));
                chunkPathOffXs[i] = (sbyte)random.Next(-6, 7);
                chunkPathOffYs[i] = (sbyte)random.Next(-6, 7);

                chunkConnections[i] = random.Next(5) <= 1 ? ConnectionFlags.Used : ConnectionFlags.NotUsed;
                if (chunkConnections[i] == 0) FreeChunkIndices.Add(i);
            }

            // Connect
            for (var j = 0; j < ChunkCount; j++)
            {
                for (var i = 0; i < ChunkCount; i++)
                {
                    var self = (ConnectionFlags)chunkConnections[j * ChunkCount + i];
                    if (self == 0) continue;

                    var iLeft = i > 0 ? i - 1 : ChunkCount - 1;
                    var iRight = i < ChunkCount - 1 ? i + 1 : 0;
                    var jAbove = j > 0 ? j - 1 : ChunkCount - 1;
                    var jBelow = j < ChunkCount - 1 ? j + 1 : 0;

                    var left = (ConnectionFlags)chunkConnections[j * ChunkCount + iLeft];
                    if (left != 0) self |= ConnectionFlags.Left;

                    var right = (ConnectionFlags)chunkConnections[j * ChunkCount + iRight];
                    if (right != 0) self |= ConnectionFlags.Right;

                    var above = (ConnectionFlags)chunkConnections[jAbove * ChunkCount + i];
                    if (above != 0) self |= ConnectionFlags.Above;

                    var below = (ConnectionFlags)chunkConnections[jBelow * ChunkCount + i];
                    if (below != 0) self |= ConnectionFlags.Below;

                    chunkConnections[j * ChunkCount + i] = self;
                }
            }

            for (var j = 0; j < ChunkCount; j++)
            {
                for (var i = 0; i < ChunkCount; i++)
                {
                    var self = (ConnectionFlags)chunkConnections[j * ChunkCount + i];
                    if (self == 0) continue;

                    if ((self & ConnectionFlags.Right) != 0)
                    {
                        var iRight = i < ChunkCount - 1 ? i + 1 : 0;

                        DrawSupercoverLine(
                            i * ChunkSize + ChunkSize / 2 + chunkPathOffXs[j * ChunkCount + i],
                            j * ChunkSize + ChunkSize / 2 + chunkPathOffYs[j * ChunkCount + i],
                            (i + 1) * ChunkSize + ChunkSize / 2 + chunkPathOffXs[j * ChunkCount + iRight],
                            j * ChunkSize + ChunkSize / 2 + chunkPathOffYs[j * ChunkCount + iRight],
                            chunkPathSizes[j * ChunkCount + i], chunkPathSizes[j * ChunkCount + iRight]);
                    }

                    if ((self & ConnectionFlags.Below) != 0)
                    {
                        var jBelow = j < ChunkCount - 1 ? j + 1 : 0;

                        DrawSupercoverLine(
                            i * ChunkSize + ChunkSize / 2 + chunkPathOffXs[j * ChunkCount + i],
                            j * ChunkSize + ChunkSize / 2 + chunkPathOffYs[j * ChunkCount + i],
                            i * ChunkSize + ChunkSize / 2 + chunkPathOffXs[jBelow * ChunkCount + i],
                            (j + 1) * ChunkSize + ChunkSize / 2 + chunkPathOffYs[jBelow * ChunkCount + i],
                            chunkPathSizes[j * ChunkCount + i], chunkPathSizes[jBelow * ChunkCount + i]);
                    }
                }
            }

            for (var y = 0; y < MapSize; y++)
            {
                for (var x = 0; x < MapSize; x++)
                {
                    var rarity = HasNearby(x, y, Tile.Path) ? 2000 : 500;
                    if (random.Next(rarity) == 0) PokeCircle(x, y, Tile.Crystal, random.Next(1, 4));
                }
            }

            bool HasNearby(int x, int y, Tile tile, int radius = 2)
            {
                for (var j = y - radius; j <= y + radius; j++)
                {
                    for (var i = x - radius; i <= x + radius; i++)
                    {
                        if (PeekTile(i, j) == tile) return true;
                    }
                }

                return false;
            }

            void DrawSupercoverLine(int x1, int y1, int x2, int y2, int size1, int size2)
            {
                var distanceX = Math.Abs(x2 - x1);
                var distanceY = Math.Abs(y2 - y1);

                var directionX = Math.Sign(x2 - x1);
                var directionY = Math.Sign(y2 - y1);

                var x = x1;
                var y = y1;

                PokeTile(x, y, Tile.Path);

                while (true)
                {
                    var progressX = distanceX == 0 ? 1f : (float)Math.Abs(x - x1) / distanceX;
                    var progressY = distanceY == 0 ? 1f : (float)Math.Abs(y - y1) / distanceY;

                    if (progressX == 1f && progressY == 1f) break;

                    if (progressX < progressY || (progressX == progressY && distanceX > distanceY)) x += directionX;
                    else y += directionY;

                    var size = (int)(size1 + (float)Math.Min(progressX, progressY) * (size2 - size1));

                    for (var v = 0; v < size; v++)
                    {
                        for (var u = 0; u < size; u++)
                        {
                            PokeTile(x - size / 2 + u, y - size / 2 + v, Tile.Path);
                        }
                    }
                }
            }
        }

        public void PokeCircle(int x, int y, Tile tile, int radius)
        {
            for (var j = y - radius; j <= y + radius; j++)
            {
                for (var i = x - radius; i <= x + radius; i++)
                {
                    if (MathF.Ceiling(MathF.Pow(i - x, 2) + MathF.Pow(j - y, 2)) <= radius * radius) PokeTile(i, j, tile);
                }
            }
        }

        void PokeTile(int x, int y, Tile tile)
        {
            if (x < 0) x += MapSize;
            if (x >= MapSize) x -= MapSize;

            if (y < 0) y += MapSize;
            if (y >= MapSize) y -= MapSize;

            Tiles[y * MapSize + x] = (byte)tile;
        }

        public Tile PeekTile(int x, int y)
        {
            if (x < 0) x += MapSize;
            if (x >= MapSize) x -= MapSize;

            if (y < 0) y += MapSize;
            if (y >= MapSize) y -= MapSize;

            return (Tile)Tiles[y * MapSize + x];
        }

        readonly List<Entity> _upcomingEntities = new List<Entity>();

        public Entity MakeEntity(Entity.EntityType type, int playerIndex, int x, int y, Entity.EntityDirection direction)
        {
            var entity = new Entity
            {
                Id = NextEntityId++,
                Type = type,
                PlayerIndex = playerIndex,
                X = x,
                Y = y,
                Direction = direction
            };

            _upcomingEntities.Add(entity);
            return entity;
        }

        public void AddUpcomingEntities()
        {
            foreach (var entity in _upcomingEntities)
            {
                Entities.Add(entity);
                EntitiesById.Add(entity.Id, entity);
                if (entity.PlayerIndex != -1) PlayersInOrder[entity.PlayerIndex].OwnedEntities.Add(entity);
            }

            _upcomingEntities.Clear();
        }

        public Entity PeekEntity(int x, int y)
        {
            // TODO: Optimize with space partitioning
            for (var i = 0; i < Entities.Count; i++)
            {
                if (Entities[i].X == x && Entities[i].Y == y) return Entities[i];
            }

            return null;
        }

        // http://www.roguebasin.com/index.php?title=Bresenham%27s_Line_Algorithm
        public bool HasLineOfSight(int x0, int y0, int x1, int y1)
        {
            static void Swap<T>(ref T lhs, ref T rhs) { T temp; temp = lhs; lhs = rhs; rhs = temp; }
            bool IsTileTransparent(int x, int y) => PeekTile(x, y) == Tile.Path;

            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            if (steep) { Swap(ref x0, ref y0); Swap(ref x1, ref y1); }
            if (x0 > x1) { Swap(ref x0, ref x1); Swap(ref y0, ref y1); }
            int dX = (x1 - x0), dY = Math.Abs(y1 - y0), err = (dX / 2), ystep = (y0 < y1 ? 1 : -1), y = y0;

            for (int x = x0; x <= x1; ++x)
            {
                if (!(steep ? IsTileTransparent(y, x) : IsTileTransparent(x, y))) return false;
                err -= dY;
                if (err < 0) { y += ystep; err += dX; }
            }

            return true;
        }
    }
}
