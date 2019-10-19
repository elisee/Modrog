using System;

namespace ModrogServer
{
    class PeerIdentity
    {
        public Guid Guid;
        public string Name;

        public bool IsHost;
        public bool IsOnline;
        public bool IsReady;

        public int PlayerIndex = -1;
    }
}
