using System;
using System.Collections.Generic;
using System.IO;

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
            Rock = 0,
            Path = 1,
            Crystal = 2
        }

        public static readonly uint[] TileColors = new uint[] {
            0x51260aff,
            0x271104ff,
            0xa8d618ff
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
        public readonly List<Robot> Robots = new List<Robot>();

        public void LoadFromFile(string path)
        {
            using (var file = File.OpenRead(path))
            using (var mapReader = new BinaryReader(file))
            {
                mapReader.Read(Tiles, 0, Tiles.Length);
                var freeChunksCount = mapReader.ReadInt32();
                for (var i = 0; i < freeChunksCount; i++) FreeChunkIndices.Add(mapReader.ReadInt32());

                // TODO: Load players & robots
            }
        }

        public void SaveToFile(string path)
        {
            using (var file = File.OpenWrite(path))
            using (var mapWriter = new BinaryWriter(file))
            {
                mapWriter.Write(Tiles, 0, Tiles.Length);
                mapWriter.Write(FreeChunkIndices.Count);
                foreach (var freeChunkIndex in FreeChunkIndices) mapWriter.Write(freeChunkIndex);

                // TODO: Save players & robots
            }
        }

        public void Generate()
        {
            Array.Clear(Tiles, 0, Tiles.Length);
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
                    var index = y * MapSize + x;
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
                        if (Peek(i, j) == tile) return true;
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

                Poke(x, y, Tile.Path);

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
                            Poke(x - size / 2 + u, y - size / 2 + v, Tile.Path);
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
                    if (Math.Ceiling(Math.Pow(i - x, 2) + Math.Pow(j - y, 2)) <= radius * radius) Poke(i, j, tile);
                }
            }
        }

        void Poke(int x, int y, Tile tile)
        {
            if (x < 0) x += MapSize;
            if (x >= MapSize) x -= MapSize;

            if (y < 0) y += MapSize;
            if (y >= MapSize) y -= MapSize;

            Tiles[y * MapSize + x] = (byte)tile;
        }

        Tile Peek(int x, int y)
        {
            if (x < 0) x += MapSize;
            if (x >= MapSize) x -= MapSize;

            if (y < 0) y += MapSize;
            if (y >= MapSize) y -= MapSize;

            return (Tile)Tiles[y * MapSize + x];
        }
    }
}
