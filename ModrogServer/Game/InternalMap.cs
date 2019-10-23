using ModrogCommon;
using SwarmBasics.Math;
using System.Collections.Generic;

namespace ModrogServer.Game
{
    sealed class InternalMap : ModrogApi.Server.Map
    {
        internal readonly Dictionary<Point, Chunk> Chunks = new Dictionary<Point, Chunk>();
    }
}
