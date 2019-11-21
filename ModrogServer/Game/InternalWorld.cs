using ModrogCommon;
using SwarmBasics.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ModrogServer.Game
{
    sealed class InternalWorld : ModrogApi.Server.World
    {
        internal readonly InternalUniverse Universe;

        internal readonly Dictionary<Point, Chunk> Chunks = new Dictionary<Point, Chunk>();

        readonly List<InternalEntity> _entities = new List<InternalEntity>();

        internal InternalWorld(InternalUniverse universe)
        {
            Universe = universe;
        }

        internal void Tick()
        {
            var entities = _entities.ToArray();

            foreach (var entity in entities)
            {
                if (entity.World != this) continue;

                entity.PreviousTickPosition = entity.Position;
                entity.Action = ModrogApi.EntityAction.Idle;
                entity.ActionDirection = ModrogApi.Direction.Down;
                entity.ActionItem = null;

                if (entity.Intent == ModrogApi.CharacterIntent.Idle) continue;

                var intent = entity.Intent;
                entity.Intent = ModrogApi.CharacterIntent.Idle;

                Universe._script.OnCharacterIntent(entity, intent, entity.IntentDirection, entity.IntentSlot, out var preventDefault);
                if (preventDefault) continue;

                switch (intent)
                {
                    case ModrogApi.CharacterIntent.Move:
                        {
                            var newPosition = entity.Position + ModrogApi.MathHelper.GetOffsetFromDirection(entity.IntentDirection);
                            entity.ActionDirection = entity.IntentDirection;

                            // TODO: Need to check each layer for various flags
                            var targetTile = PeekTile(ModrogApi.MapLayer.Wall, newPosition);
                            if (targetTile != 0)
                            {
                                entity.Action = ModrogApi.EntityAction.Bounce;
                                break;
                            }

                            entity.Action = ModrogApi.EntityAction.Move;
                            entity.Position = newPosition;

                            // TODO: Dig, push, collect, etc.
                            // TODO: Check for interactions with entities
                        }
                        break;

                    case ModrogApi.CharacterIntent.Use:
                        break;


                    case ModrogApi.CharacterIntent.Idle:
                        break;
                }
            }
        }

        public override ModrogApi.Server.Entity SpawnCharacter(ModrogApi.Server.CharacterKind kind, Point position, ModrogApi.Server.Player owner)
        {
            return new InternalEntity(Universe.GetNextEntityId(), this, ((InternalCharacterKind)kind).SpriteLocation, position, owner != null ? ((InternalPlayer)owner).Index : -1);
        }

        public override ModrogApi.Server.Entity SpawnItem(ModrogApi.Server.ItemKind kind, Point position)
        {
            return new InternalEntity(Universe.GetNextEntityId(), this, ((InternalItemKind)kind).SpriteLocation, position, -1);
        }

        internal void Add(InternalEntity entity)
        {
            Debug.Assert(entity.World == null);
            _entities.Add(entity);
            entity.World = this;
        }

        internal void Remove(InternalEntity entity)
        {
            Debug.Assert(entity.World == this);
            _entities.Remove(entity);
            entity.World = null;
        }

        internal short PeekTile(ModrogApi.MapLayer layer, Point position)
        {
            var chunkCoords = new Point(
                (int)MathF.Floor((float)position.X / Protocol.MapChunkSide),
                (int)MathF.Floor((float)position.Y / Protocol.MapChunkSide));

            if (!Chunks.TryGetValue(chunkCoords, out var chunk)) return 0;

            var chunkTileCoords = new Point(
                MathHelper.Mod(position.X, Protocol.MapChunkSide),
                MathHelper.Mod(position.Y, Protocol.MapChunkSide));

            return chunk.TilesPerLayer[(int)layer][chunkTileCoords.Y * Protocol.MapChunkSide + chunkTileCoords.X];
        }

        internal short[] PeekTileStack(Point position)
        {
            var chunkCoords = new Point(
                (int)MathF.Floor((float)position.X / Protocol.MapChunkSide),
                (int)MathF.Floor((float)position.Y / Protocol.MapChunkSide));

            var stack = new short[(int)ModrogApi.MapLayer.Count];
            if (!Chunks.TryGetValue(chunkCoords, out var chunk)) return stack;

            var chunkTileCoords = new Point(
                MathHelper.Mod(position.X, Protocol.MapChunkSide),
                MathHelper.Mod(position.Y, Protocol.MapChunkSide));

            for (var i = 0; i < (int)ModrogApi.MapLayer.Count; i++) stack[i] = chunk.TilesPerLayer[i][chunkTileCoords.Y * Protocol.MapChunkSide + chunkTileCoords.X];

            return stack;
        }

        public override IReadOnlyList<ModrogApi.Server.Entity> GetEntities(Point position)
        {
            var entities = new List<ModrogApi.Server.Entity>();

            // TODO: Optimize with space partitioning
            foreach (var entity in _entities)
            {
                if (entity.Position == position) entities.Add(entity);
            }

            return entities;
        }

        // http://www.roguebasin.com/index.php?title=Bresenham%27s_Line_Algorithm
        internal bool HasLineOfSight(int x0, int y0, int x1, int y1)
        {
            static void Swap<T>(ref T lhs, ref T rhs) { T temp; temp = lhs; lhs = rhs; rhs = temp; }
            bool IsTileTransparent(int x, int y) => PeekTile(ModrogApi.MapLayer.Wall, new Point(x, y)) == 0;

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

        #region API
        public override void SetTile(ModrogApi.MapLayer layer, Point position, ModrogApi.Server.TileKind tileKind)
        {
            var chunkCoords = new Point(
                (int)MathF.Floor((float)position.X / Protocol.MapChunkSide),
                (int)MathF.Floor((float)position.Y / Protocol.MapChunkSide));

            if (!Chunks.TryGetValue(chunkCoords, out var chunk))
            {
                chunk = new Chunk((int)ModrogApi.MapLayer.Count);
                Chunks.Add(chunkCoords, chunk);
            }

            var chunkTileCoords = new Point(
                MathHelper.Mod(position.X, Protocol.MapChunkSide),
                MathHelper.Mod(position.Y, Protocol.MapChunkSide));

            chunk.TilesPerLayer[(int)layer][chunkTileCoords.Y * Protocol.MapChunkSide + chunkTileCoords.X] = (short)(1 + ((InternalTileKind)tileKind).Index);
        }

        public override void InsertMap(int offX, int offY, ModrogApi.Server.Map map)
        {
            var internalMap = (InternalMap)map;

            foreach (var (chunkCoords, mapChunk) in internalMap.Chunks)
            {
                var worldStartTileCoords = new Point(
                    chunkCoords.X * Protocol.MapChunkSide + offX,
                    chunkCoords.Y * Protocol.MapChunkSide + offY);

                var worldEndTileCoords = new Point(
                    worldStartTileCoords.X + Protocol.MapChunkSide - 1,
                    worldStartTileCoords.Y + Protocol.MapChunkSide - 1);

                var worldStartChunkCoords = new Point(
                    (int)MathF.Floor((float)worldStartTileCoords.X / Protocol.MapChunkSide),
                    (int)MathF.Floor((float)worldStartTileCoords.Y / Protocol.MapChunkSide));

                var worldEndChunkCoords = new Point(
                    (int)MathF.Floor((float)worldEndTileCoords.X / Protocol.MapChunkSide),
                    (int)MathF.Floor((float)worldEndTileCoords.Y / Protocol.MapChunkSide));

                for (var chunkY = worldStartChunkCoords.Y; chunkY <= worldEndChunkCoords.Y; chunkY++)
                {
                    for (var chunkX = worldStartChunkCoords.X; chunkX <= worldEndChunkCoords.X; chunkX++)
                    {
                        var worldChunkCoords = new Point(chunkX, chunkY);

                        if (!Chunks.TryGetValue(worldChunkCoords, out var worldChunk))
                        {
                            worldChunk = new Chunk((int)ModrogApi.MapLayer.Count);
                            Chunks.Add(worldChunkCoords, worldChunk);
                        }

                        var worldChunkStartTileCoords = new Point(chunkX * Protocol.MapChunkSide, chunkY * Protocol.MapChunkSide);

                        var worldChunkRelativeStartTileCoords = new Point(
                            Math.Max(0, worldStartTileCoords.X - worldChunkStartTileCoords.X),
                            Math.Max(0, worldStartTileCoords.Y - worldChunkStartTileCoords.Y));

                        var worldChunkRelativeEndTileCoords = new Point(
                            Math.Min(Protocol.MapChunkSide, worldEndTileCoords.X - worldChunkStartTileCoords.X),
                            Math.Min(Protocol.MapChunkSide, worldEndTileCoords.Y - worldChunkStartTileCoords.Y));

                        for (var worldChunkRelativeY = worldChunkRelativeStartTileCoords.Y; worldChunkRelativeY <= worldChunkRelativeEndTileCoords.Y; worldChunkRelativeY++)
                        {
                            for (var worldChunkRelativeX = worldChunkRelativeStartTileCoords.X; worldChunkRelativeX <= worldChunkRelativeEndTileCoords.X; worldChunkRelativeX++)
                            {
                                for (var tileLayer = 0; tileLayer < (int)ModrogApi.MapLayer.Count; tileLayer++)
                                {
                                    var mapTileCoords = new Point(worldChunkRelativeX - offX, worldChunkRelativeY - offY);
                                    var tile = mapChunk.TilesPerLayer[tileLayer][mapTileCoords.Y * Protocol.MapChunkSide + mapTileCoords.X];
                                    worldChunk.TilesPerLayer[tileLayer][worldChunkRelativeY * Protocol.MapChunkSide + worldChunkRelativeX] = tile;
                                }
                            }
                        }

                    }
                }
            }
        }
        #endregion
    }
}