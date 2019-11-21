using SwarmBasics.Math;
using System;
using System.Diagnostics;
using System.Text;

namespace SwarmBasics.Packets
{
    public class PacketWriter
    {
        public byte[] Buffer => _buffer;
        byte[] _buffer;
        int _cursor;
        bool _useSizeHeader = false;

        public PacketWriter(int initialCapacity, bool useSizeHeader)
        {
            _buffer = new byte[initialCapacity];

            _useSizeHeader = useSizeHeader;
            if (useSizeHeader) _cursor = sizeof(int);
        }

        public int Finish()
        {
            if (!_useSizeHeader) return _cursor;

            var length = _cursor - sizeof(int);
            _cursor = 0;
            WriteInt(length);
            return length + sizeof(int);
        }

        void EnsureBytesAvailable(int bytes)
        {
            if (_cursor + bytes > _buffer.Length) Array.Resize(ref _buffer, (int)System.Math.Ceiling((_buffer.Length + bytes) * 1.5));
        }

        public void WriteByte(byte value)
        {
            EnsureBytesAvailable(sizeof(byte));

            _buffer[_cursor] = value;
            _cursor += sizeof(byte);
        }

        public void WriteShort(short value)
        {
            EnsureBytesAvailable(sizeof(short));

            _buffer[_cursor + 0] = (byte)((value >> 8) & 0xff);
            _buffer[_cursor + 1] = (byte)((value >> 0) & 0xff);
            _cursor += sizeof(short);
        }

        public void WriteShortPoint(Point value)
        {
            EnsureBytesAvailable(sizeof(short) * 2);

            _buffer[_cursor + 0] = (byte)((value.X >> 8) & 0xff);
            _buffer[_cursor + 1] = (byte)((value.X >> 0) & 0xff);
            _buffer[_cursor + 2] = (byte)((value.Y >> 8) & 0xff);
            _buffer[_cursor + 3] = (byte)((value.Y >> 0) & 0xff);
            _cursor += sizeof(short) * 2;
        }

        public void WriteInt(int value)
        {
            EnsureBytesAvailable(sizeof(int));

            _buffer[_cursor + 0] = (byte)((value >> 24) & 0xff);
            _buffer[_cursor + 1] = (byte)((value >> 16) & 0xff);
            _buffer[_cursor + 2] = (byte)((value >> 8) & 0xff);
            _buffer[_cursor + 3] = (byte)((value >> 0) & 0xff);
            _cursor += sizeof(int);
        }

        public void WriteByteSizeString(string value)
        {
            var sizeInBytes = Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _cursor + sizeof(byte));
            EnsureBytesAvailable(sizeof(byte) + sizeInBytes);

            Debug.Assert(sizeInBytes <= byte.MaxValue);
            WriteByte((byte)sizeInBytes);
            _cursor += sizeInBytes;
        }

        public void WriteShortSizeString(string value)
        {
            var sizeInBytes = Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _cursor + sizeof(short));
            EnsureBytesAvailable(sizeof(short) + sizeInBytes);

            Debug.Assert(sizeInBytes <= short.MaxValue);
            WriteShort((short)sizeInBytes);
            _cursor += sizeInBytes;
        }

        public void WriteBytes(byte[] value)
        {
            EnsureBytesAvailable(value.Length);

            System.Buffer.BlockCopy(value, 0, _buffer, _cursor, value.Length);
            _cursor += value.Length;
        }

        public void WriteShorts(short[] value)
        {
            EnsureBytesAvailable(value.Length * sizeof(short));

            System.Buffer.BlockCopy(value, 0, _buffer, _cursor, value.Length * sizeof(short));
            _cursor += value.Length * sizeof(short);
        }
    }
}
