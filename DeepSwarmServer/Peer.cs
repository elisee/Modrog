using DeepSwarmCommon;
using System.Net.Sockets;

namespace DeepSwarmServer
{
    class Peer
    {
        public readonly PacketReceiver Receiver;

        public Socket Socket;
        public PeerIdentity Identity;

        public Peer(Socket socket)
        {
            Socket = socket;
            Receiver = new PacketReceiver(Socket);
        }
    }
}
