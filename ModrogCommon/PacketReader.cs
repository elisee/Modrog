using System;
using System.Text;

namespace ModrogCommon
{
    public class PacketReader
    {
        byte[] _buffer;
        int _cursor;

        public void Open(byte[] packet)
        {
            _buffer = packet;
            _cursor = 0;
        }

        void EnsureBytesAvailable(int bytes)
        {
            if (_cursor + bytes > _buffer.Length) throw new PacketException($"Not enough bytes left in packet (want {bytes} but got {_buffer.Length - _cursor} left)");
        }

        public byte ReadByte()
        {
            EnsureBytesAvailable(sizeof(byte));
            var value = _buffer[_cursor];
            _cursor += sizeof(byte);
            return value;
        }

        public byte ReadByte(int max)
        {
            var value = ReadByte();
            if (value > max) throw new Exception($"Read {value} but expected at most {max}");
            return value;
        }

        public TEnum ReadByte<TEnum>() where TEnum : Enum
        {
            var value = ReadByte();
            if (!Enum.IsDefined(typeof(TEnum), value)) throw new PacketException($"Invalid {typeof(TEnum).Name} enum value");
            return (TEnum)Enum.ToObject(typeof(TEnum), value);
        }

        public Span<byte> ReadBytes(int size)
        {
            EnsureBytesAvailable(size);
            var value = new Span<byte>(_buffer, _cursor, size);
            _cursor += size;
            return value;
        }

        public short ReadShort()
        {
            EnsureBytesAvailable(sizeof(short));
            var value = (short)((_buffer[_cursor + 0] << 8) + _buffer[_cursor + 1]);
            _cursor += sizeof(short);
            return value;
        }

        public int ReadInt()
        {
            EnsureBytesAvailable(sizeof(int));
            var value = (int)(_buffer[_cursor + 0] << 24) + (int)(_buffer[_cursor + 1] << 16) + (int)(_buffer[_cursor + 2] << 8) + (int)_buffer[_cursor + 3];
            _cursor += sizeof(int);
            return value;
        }

        public string ReadByteSizeString()
        {
            var sizeInBytes = ReadByte();
            EnsureBytesAvailable(sizeInBytes);
            var value = Encoding.UTF8.GetString(_buffer, _cursor, sizeInBytes);
            _cursor += sizeInBytes;
            return value;
        }

        public string ReadShortSizeString()
        {
            var sizeInBytes = ReadShort();
            EnsureBytesAvailable(sizeInBytes);
            var value = Encoding.UTF8.GetString(_buffer, _cursor, sizeInBytes);
            _cursor += sizeInBytes;
            return value;
        }
    }
}
