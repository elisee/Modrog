namespace ModrogCommon
{
    public class Chunk
    {
        public readonly short[][] TilesPerLayer;

        public Chunk(short[][] tilesPerLayer)
        {
            TilesPerLayer = tilesPerLayer;
        }

        public Chunk(int layerCount)
        {
            TilesPerLayer = new short[layerCount][];
            for (var i = 0; i < TilesPerLayer.Length; i++) TilesPerLayer[i] = new short[Protocol.MapChunkSide * Protocol.MapChunkSide];
        }
    }
}
