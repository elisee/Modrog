using DeepSwarmCommon;
using System.Net.Sockets;

namespace DeepSwarmServer
{
    class Peer
    {
        public readonly PacketReceiver Receiver;

        public Socket Socket;
        public PlayerIdentity Identity;

        public Peer(Socket socket)
        {
            Socket = socket;
            Receiver = new PacketReceiver(Socket);
        }
    }
}
