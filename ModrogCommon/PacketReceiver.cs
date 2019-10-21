using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ModrogCommon
{
    public class PacketReceiver
    {
        public Socket _socket;

        byte[] _buffer = new byte[uint.MaxValue];
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
            if (_offset <= sizeof(int)) return true;

            while (_offset >= sizeof(int))
            {
                var packetSize = (_buffer[0] << 24) + (_buffer[1] << 16) + (_buffer[2] << 8) + _buffer[3];
                var endOfPacketOffset = sizeof(int) + packetSize;

                if (_offset < endOfPacketOffset) break;

                var packet = new byte[packetSize];
                Buffer.BlockCopy(_buffer, sizeof(int), packet, 0, packetSize);

                Buffer.BlockCopy(_buffer, endOfPacketOffset, _buffer, 0, _offset - endOfPacketOffset);
                _offset -= endOfPacketOffset;

                packets.Add(packet);
            }

            return true;
        }
    }
}
