using System;

namespace ModrogApi.Server
{
    [Flags]
    public enum EntityCapabilities
    {
        Move = 1 << 0,
        Attack = 1 << 1,
        Push = 1 << 2,
        Pushable = 1 << 3,
    }
}