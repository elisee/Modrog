using System;

namespace SwarmBasics.Packets
{
    public class PacketException : Exception
    {
        public PacketException(string message) : base(message)
        {
        }
    }
}
