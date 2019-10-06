using DeepSwarmCommon;
using System.IO;

namespace DeepSwarmServer
{
    partial class ServerState
    {
        void StartPlaying()
        {
            var peers = new Peer[_identifiedPeerSockets.Count];
            var players = new Game.Player[peers.Length];

            for (var i = 0; i < peers.Length; i++)
            {
                peers[i] = _peersBySocket[_identifiedPeerSockets[i]];
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

            foreach (var socket in _identifiedPeerSockets)
            {
                var peer = _peersBySocket[socket];
                var player = _universe.PlayersById[peer.Identity.PlayerId];

                _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Tick);
                // TODO: Write player-specific data
                Send(peer.Socket);
            }
        }
    }
}
