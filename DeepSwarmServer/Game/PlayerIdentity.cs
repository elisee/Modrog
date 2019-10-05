using System;

namespace DeepSwarmServer.Game
{
    class PlayerIdentity
    {
        public Guid Guid;
        public string Name;
        public bool IsHost;
        public bool IsOnline;
        public bool IsReady;
    }
}
