using ModrogCommon;
using System.Net.Sockets;

namespace ModrogServer
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
