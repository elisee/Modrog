using System;

namespace DeepSwarmApi.Server
{
    [Flags]
    public enum EntityCapabilities
    {
        Move,
        Attack,
        Push,
        Pushable
    }
}