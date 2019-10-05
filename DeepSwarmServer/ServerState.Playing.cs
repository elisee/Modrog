using DeepSwarmCommon;
using System.IO;

namespace DeepSwarmServer
{
    partial class ServerState
    {
        void StartPlaying()
        {
            var peers = new Peer[_peersBySocket.Count];
            _peersBySocket.Values.CopyTo(peers, 0);

            var players = new Game.Player[peers.Length];
            for (var i = 0; i < peers.Length; i++)
            {
                peers[i].Identity.PlayerId = i;
                players[i] = new Game.Player { Id = i, Name = peers[i].Identity.Name };
            }

            BroadcastPeerList();

            _universe = new Game.Universe(players, Path.Combine(_scenariosPath, _scenarioName));
            _stage = ServerStage.Playing;
        }

        void Tick()
        {
            _universe.Tick();

            foreach (var peer in _peersBySocket.Values)
            {
                var player = _universe.PlayersById[peer.Identity.PlayerId];

                _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Tick);
                // TODO: Write player-specific data
                Send(peer.Socket);
            }
        }
    }
}
