using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace DeepSwarmCommon
{
    public class PacketReceiver
    {
        public Socket _socket;

        byte[] _buffer = new byte[ushort.MaxValue];
        int _offset;

        public PacketReceiver(Socket socket)
        {
            _socket = socket;
        }

        public bool Read(out List<byte[]> packets)
        {
            var bytesRead = 0;
            try { bytesRead = _socket.Receive(_buffer, _offset, _buffer.Length - _offset, SocketFlags.None); } catch (SocketException) { }

            if (bytesRead == 0)
            {
                packets = null;
                return false;
            }

            packets = new List<byte[]>();

            _offset += bytesRead;
            if (_offset <= 2) return true;

            while (_offset >= 2)
            {
                var packetSize = (_buffer[0] << 8) + _buffer[1];
                var endOfPacketOffset = 2 + packetSize;

                if (_offset < endOfPacketOffset) break;

                var packet = new byte[packetSize];
                Buffer.BlockCopy(_buffer, 2, packet, 0, packetSize);

                Buffer.BlockCopy(_buffer, endOfPacketOffset, _buffer, 0, _offset - endOfPacketOffset);
                _offset -= endOfPacketOffset;

                packets.Add(packet);
            }

            return true;
        }
    }
}
