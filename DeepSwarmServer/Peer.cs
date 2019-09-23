using DeepSwarmCommon;
using System.Net.Sockets;

namespace DeepSwarmServer
{
    class Peer
    {
        public readonly PacketReceiver Receiver;

        public enum PeerStage
        {
            WaitingForHandshake,
            Playing,
        }

        public Peer(Socket socket)
        {
            Socket = socket;
            Receiver = new PacketReceiver(Socket);
        }

        public Socket Socket;
        public PeerStage Stage;

        public Player Player;
    }
}
