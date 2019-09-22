using System.Diagnostics;
using System.Text;

namespace DeepSwarmCommon
{
    public class PacketWriter
    {
        public readonly byte[] Buffer;
        public int Cursor { get; private set; }

        public PacketWriter(int size = 8192)
        {
            Buffer = new byte[size];
        }

        public void ResetCursor()
        {
            Cursor = 0;
        }

        public void WriteByte(byte value)
        {
            Buffer[Cursor] = value;
            Cursor += sizeof(byte);
        }

        public void WriteShort(short value)
        {
            Buffer[Cursor + 0] = (byte)((value >> 8) & 0xff);
            Buffer[Cursor + 1] = (byte)((value >> 0) & 0xff);
            Cursor += sizeof(short);
        }

        public void WriteInt(int value)
        {
            Buffer[Cursor + 0] = (byte)((value >> 24) & 0xff);
            Buffer[Cursor + 1] = (byte)((value >> 16) & 0xff);
            Buffer[Cursor + 2] = (byte)((value >> 8) & 0xff);
            Buffer[Cursor + 3] = (byte)((value >> 0) & 0xff);
            Cursor += sizeof(int);
        }

        public void WriteByteLengthString(string value)
        {
            var sizeInBytes = Encoding.UTF8.GetBytes(value, 0, value.Length, Buffer, Cursor + 1);
            Debug.Assert(sizeInBytes <= byte.MaxValue);
            WriteByte((byte)sizeInBytes);
            Cursor += sizeInBytes;
        }

        public void WriteBytes(byte[] value)
        {
            System.Buffer.BlockCopy(value, 0, Buffer, Cursor, value.Length);
            Cursor += value.Length;
        }
    }
}
