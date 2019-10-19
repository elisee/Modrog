using System;

namespace ModrogCommon
{
    public class PacketException : Exception
    {
        public PacketException(string message) : base(message)
        {
        }
    }
}
