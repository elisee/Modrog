using System;

namespace DeepSwarmCommon
{
    public class PacketException : Exception
    {
        public PacketException(string message) : base(message)
        {
        }
    }
}
