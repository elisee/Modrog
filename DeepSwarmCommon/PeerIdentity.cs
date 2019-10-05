using System;

namespace DeepSwarmServer
{
    class PeerIdentity
    {
        public int Id;
        public Guid SecretGuid;
        public string Name;
        public bool IsHost;
        public bool IsOnline;
        public bool IsReady;
    }
}
