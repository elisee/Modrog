using DeepSwarmCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace DeepSwarmServer
{
    class Program
    {
        static void Main()
        {
            var random = new Random();

            var mapFilePath = Path.Combine(AppContext.BaseDirectory, "Map.dat");
            var map = new Map();

            if (File.Exists(mapFilePath))
            {
                Console.WriteLine($"Loading map from {mapFilePath}...");
                map.LoadFromFile(mapFilePath);
                Console.WriteLine($"Done loading map.");
            }
            else
            {
                Console.WriteLine($"Generating map, saving to {mapFilePath}...");
                map.Generate();
                map.SaveToFile(mapFilePath);
                Console.WriteLine($"Done generating map.");
            }

            var listenerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                LingerState = new LingerOption(true, seconds: 1)
            };

            listenerSocket.Bind(new IPEndPoint(IPAddress.Any, Protocol.Port));
            listenerSocket.Listen(64);
            Console.WriteLine($"Server listening on port {Protocol.Port}.");

            var peerSockets = new List<Socket>();
            var peersBySocket = new Dictionary<Socket, Peer>();
            var peersByGuid = new Dictionary<Guid, Peer>();
            var playingPeers = new List<Peer>();

            var pollSockets = new List<Socket>();
            var writer = new PacketWriter();
            var reader = new PacketReader();

            while (true)
            {
                pollSockets.Clear();
                pollSockets.Add(listenerSocket);
                pollSockets.AddRange(peerSockets);

                Socket.Select(pollSockets, null, null, 0);

                foreach (var readSocket in pollSockets)
                {
                    if (readSocket == listenerSocket)
                    {
                        var newSocket = listenerSocket.Accept();
                        peerSockets.Add(newSocket);
                        peersBySocket.Add(newSocket, new Peer { Socket = newSocket });
                        Console.WriteLine($"{newSocket.RemoteEndPoint} - Socket connected.");
                    }
                    else
                    {
                        Read(readSocket);
                    }
                }
            }

            void Read(Socket socket)
            {
                var peer = peersBySocket[socket];

                int bytesRead;
                try { bytesRead = socket.Receive(reader.Buffer); }
                catch (SocketException) { KickPeer(null); return; }
                if (bytesRead == 0) { KickPeer(null); return; }

                void KickPeer(string kickReason)
                {
                    var endPoint = socket.RemoteEndPoint;
                    socket.Close();
                    peerSockets.Remove(socket);
                    peersBySocket.Remove(socket);
                    if (peer.Player != null) peersByGuid.Remove(peer.Player.Guid);
                    if (peer.Stage == Peer.PeerStage.Playing)
                    {
                        playingPeers.Remove(peer);
                        BroadcastPlayerList();
                    }

                    if (kickReason == null) Console.WriteLine($"{endPoint} - Socket disconnected.");
                    else Console.WriteLine($"{endPoint} - Kicked: {kickReason}");
                }

                reader.ResetCursor();

                switch (peer.Stage)
                {
                    case Peer.PeerStage.WaitingForHandshake:
                        string versionString;
                        try { versionString = reader.ReadByteSizeString(); }
                        catch { KickPeer($"Invalid {nameof(Peer.PeerStage.WaitingForHandshake)} packet."); return; }
                        if (versionString != Protocol.VersionString) { KickPeer($"Invalid protocol string, expected {Protocol.VersionString}, got {versionString}."); return; }

                        Guid playerGuid;
                        try { playerGuid = new Guid(reader.ReadBytes(16)); if (playerGuid == Guid.Empty) throw new Exception(); }
                        catch { KickPeer($"Received invalid player Guid."); return; }

                        if (!peersByGuid.TryAdd(playerGuid, peer)) { KickPeer($"There is already someone connected with that Guid."); return; }

                        if (!map.PlayersByGuid.TryGetValue(playerGuid, out peer.Player))
                        {
                            peer.Player = new Player { Guid = playerGuid };

                            var index = random.Next(map.FreeChunkIndices.Count);
                            var chunkIndex = map.FreeChunkIndices[index];
                            map.FreeChunkIndices.RemoveAt(index);

                            peer.Player.BaseChunkX = chunkIndex % Map.ChunkCount;
                            peer.Player.BaseChunkY = chunkIndex / Map.ChunkCount;

                            map.PokeCircle(peer.Player.BaseChunkX * Map.ChunkSize + Map.ChunkSize / 2, peer.Player.BaseChunkY * Map.ChunkSize + Map.ChunkSize / 2, Map.Tile.Path, 6);
                        }

                        peer.Stage = Peer.PeerStage.WaitingForName;
                        break;

                    case Peer.PeerStage.WaitingForName:
                        string peerName;
                        try { peerName = reader.ReadByteSizeString(); }
                        catch { KickPeer($"Invalid {nameof(Peer.PeerStage.WaitingForName)} packet."); return; }
                        if (peerName.Length == 0 || peerName.Length > Protocol.MaxPlayerNameLength) { KickPeer($"Invalid player name: {peerName}."); return; }

                        peer.Player.Name = peerName;
                        Console.WriteLine($"{socket.RemoteEndPoint} - Player name set to: " + peer.Player.Name);

                        peer.Stage = Peer.PeerStage.Playing;
                        playingPeers.Add(peer);

                        BroadcastPlayerList();

                        writer.ResetCursor();
                        writer.WriteByte((byte)Protocol.ServerPacketType.Setup);
                        writer.WriteShort((short)peer.Player.BaseChunkX);
                        writer.WriteShort((short)peer.Player.BaseChunkY);
                        WriteMapArea(
                            peer.Player.BaseChunkX * Map.ChunkSize, peer.Player.BaseChunkY * Map.ChunkSize,
                            Map.ChunkSize, Map.ChunkSize);
                        Send(socket);
                        break;

                    case Peer.PeerStage.Playing:
                        break;
                }
            }

            void Broadcast()
            {
                Console.WriteLine($"Broadcasting {writer.Cursor} bytes");
                foreach (var peer in playingPeers) peer.Socket.Send(writer.Buffer, 0, writer.Cursor, SocketFlags.None);
            }

            void Send(Socket socket)
            {
                Console.WriteLine($"Sending {writer.Cursor} bytes");
                socket.Send(writer.Buffer, 0, writer.Cursor, SocketFlags.None);
            }

            void WriteMapArea(int x, int y, int width, int height)
            {
                writer.WriteShort((short)x);
                writer.WriteShort((short)y);
                writer.WriteShort((short)width);
                writer.WriteShort((short)height);

                var area = new byte[width * height];
                for (var j = 0; j < height; j++) Buffer.BlockCopy(map.Tiles, (y + j) * Map.MapSize + x, area, j * width, width);

                writer.WriteBytes(area);

                // TODO: Send robots too
            }

            void BroadcastPlayerList()
            {
                writer.ResetCursor();
                writer.WriteByte((byte)Protocol.ServerPacketType.PlayerList);
                writer.WriteInt(playingPeers.Count);

                foreach (var playingPeer in playingPeers)
                {
                    writer.WriteByteLengthString(playingPeer.Player.Name);
                    writer.WriteByte((byte)playingPeer.Player.Team);
                }

                Broadcast();
            }
        }

    }
}
