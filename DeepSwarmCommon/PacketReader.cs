using System;
using System.Text;

namespace DeepSwarmCommon
{
    public class PacketReader
    {
        public readonly byte[] Buffer;
        public int Cursor { get; private set; }

        public PacketReader(int size = 8192)
        {
            Buffer = new byte[size];
        }

        public void ResetCursor()
        {
            Cursor = 0;
        }

        void EnsureBytesAvailable(int bytes)
        {
            if (Cursor + bytes >= Buffer.Length) throw new PacketException($"Not enough bytes left in packet (want {bytes} but got {Buffer.Length - Cursor} left)");
        }

        public byte ReadByte()
        {
            EnsureBytesAvailable(sizeof(byte));
            var value = Buffer[Cursor];
            Cursor += sizeof(byte);
            return value;
        }

        public Span<byte> ReadBytes(int size)
        {
            EnsureBytesAvailable(size);
            var value = new Span<byte>(Buffer, Cursor, size);
            Cursor += size;
            return value;
        }

        public short ReadShort()
        {
            EnsureBytesAvailable(sizeof(short));
            var value = (short)((Buffer[Cursor] << 8) + Buffer[Cursor + 1]);
            Cursor += sizeof(short);
            return value;
        }

        public int ReadInt()
        {
            EnsureBytesAvailable(sizeof(int));
            var value = (int)(Buffer[Cursor + 0] << 24) + (int)(Buffer[Cursor + 1] << 16) + (int)(Buffer[Cursor + 2] << 8) + (int)Buffer[Cursor + 3];
            Cursor += sizeof(int);
            return value;
        }

        public string ReadByteSizeString()
        {
            var sizeInBytes = ReadByte();
            EnsureBytesAvailable(sizeInBytes);
            var value = Encoding.UTF8.GetString(Buffer, Cursor, sizeInBytes);
            Cursor += sizeInBytes;
            return value;
        }
    }
}
