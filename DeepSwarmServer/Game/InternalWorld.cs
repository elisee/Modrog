using DeepSwarmBasics.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DeepSwarmServer.Game
{
    public class InternalWorld : DeepSwarmApi.Server.World
    {
        internal readonly InternalUniverse Universe;

        readonly short[] _tiles;
        public readonly int Width;
        public readonly int Height;

        readonly List<InternalEntity> _entities = new List<InternalEntity>();

        internal InternalWorld(InternalUniverse universe, int width, int height)
        {
            Universe = universe;

            Width = width;
            Height = height;
            _tiles = new short[width * height];
        }

        internal void Tick()
        {
            foreach (var entity in _entities)
            {
                switch (entity.UpcomingMove)
                {
                    case DeepSwarmApi.Server.EntityMove.RotateCW:
                        entity.Direction = (DeepSwarmApi.Server.EntityDirection)((int)(entity.Direction + 1) % 4);
                        break;
                    case DeepSwarmApi.Server.EntityMove.RotateCCW:
                        entity.Direction = (DeepSwarmApi.Server.EntityDirection)((int)(entity.Direction + 3) % 4);
                        break;

                    case DeepSwarmApi.Server.EntityMove.Forward:
                        {
                            var newX = entity.Position.X;
                            var newY = entity.Position.Y;

                            switch (entity.Direction)
                            {
                                case DeepSwarmApi.Server.EntityDirection.Right: newX++; break;
                                case DeepSwarmApi.Server.EntityDirection.Down: newY++; break;
                                case DeepSwarmApi.Server.EntityDirection.Left: newX--; break;
                                case DeepSwarmApi.Server.EntityDirection.Up: newY--; break;
                            }

                            var targetTileKind = Universe.TileKinds[PeekTile(newX, newY)];

                            if (targetTileKind.Flags.HasFlag(DeepSwarmApi.Server.TileFlags.Solid))
                            {
                                // Can't move
                                break;
                            }

                            // TODO: Dig, push, collect, etc.
                            // TODO: Check for interactions with entities
                        }
                        break;

                    case DeepSwarmApi.Server.EntityMove.Idle:
                        break;
                }

                entity.UpcomingMove = DeepSwarmApi.Server.EntityMove.Idle;
            }
        }

        public override DeepSwarmApi.Server.Entity SpawnEntity(DeepSwarmApi.Server.EntityKind kind, Point position, DeepSwarmApi.Server.EntityDirection direction, DeepSwarmApi.Server.Player owner)
        {
            // TODO: Copy settings from kind
            // ((InternalEntityKind)kind).

            return new InternalEntity(Universe.GetNextEntityId(), this, position, direction, ((InternalPlayer)owner).Index);
        }

        internal void Add(InternalEntity entity)
        {
            Debug.Assert(entity.World == null);
            _entities.Add(entity);
            entity.World = this;
        }

        internal short PeekTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return 0;
            return _tiles[y * Width + x];
        }

        internal InternalEntity PeekEntity(int x, int y)
        {
            // TODO: Optimize with space partitioning
            foreach (var entity in _entities)
            {
                if (entity.Position.X == x && entity.Position.Y == y) return entity;
            }

            return null;
        }

        // http://www.roguebasin.com/index.php?title=Bresenham%27s_Line_Algorithm
        internal bool HasLineOfSight(int x0, int y0, int x1, int y1)
        {
            static void Swap<T>(ref T lhs, ref T rhs) { T temp; temp = lhs; lhs = rhs; rhs = temp; }
            bool IsTileTransparent(int x, int y) => !Universe.TileKinds[PeekTile(x, y)].Flags.HasFlag(DeepSwarmApi.Server.TileFlags.Opaque);

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
        public override void SetTile(int x, int y, DeepSwarmApi.Server.TileKind tileKind)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            _tiles[y * Width + x] = ((InternalTileKind)tileKind).Index;
        }

        #endregion
    }
}