﻿using System.Diagnostics;
using System.Text;

namespace DeepSwarmCommon
{
    public class PacketWriter
    {
        public readonly byte[] Buffer;
        int _cursor = 2;

        public PacketWriter(int size = 8192)
        {
            Buffer = new byte[size];
        }

        public int Finish()
        {
            var length = _cursor - 2;
            _cursor = 0;
            WriteShort((short)length);
            return length + 2;
        }

        public void WriteByte(byte value)
        {
            Buffer[_cursor] = value;
            _cursor += sizeof(byte);
        }

        public void WriteShort(short value)
        {
            Buffer[_cursor + 0] = (byte)((value >> 8) & 0xff);
            Buffer[_cursor + 1] = (byte)((value >> 0) & 0xff);
            _cursor += sizeof(short);
        }

        public void WriteInt(int value)
        {
            Buffer[_cursor + 0] = (byte)((value >> 24) & 0xff);
            Buffer[_cursor + 1] = (byte)((value >> 16) & 0xff);
            Buffer[_cursor + 2] = (byte)((value >> 8) & 0xff);
            Buffer[_cursor + 3] = (byte)((value >> 0) & 0xff);
            _cursor += sizeof(int);
        }

        public void WriteByteSizeString(string value)
        {
            var sizeInBytes = Encoding.UTF8.GetBytes(value, 0, value.Length, Buffer, _cursor + 1);
            Debug.Assert(sizeInBytes <= byte.MaxValue);
            WriteByte((byte)sizeInBytes);
            _cursor += sizeInBytes;
        }

        public void WriteBytes(byte[] value)
        {
            System.Buffer.BlockCopy(value, 0, Buffer, _cursor, value.Length);
            _cursor += value.Length;
        }
    }
}
