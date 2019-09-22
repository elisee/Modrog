using DeepSwarmCommon;
using System.Net.Sockets;

namespace DeepSwarmServer
{
    class Peer
    {
        public enum PeerStage
        {
            WaitingForHandshake,
            WaitingForName,
            Playing,
        }

        public Socket Socket;
        public PeerStage Stage;

        public Player Player;
    }
}
