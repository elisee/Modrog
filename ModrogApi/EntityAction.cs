namespace ModrogApi
{
    public enum EntityAction : byte
    {
        Idle,
        Teleport,
        Move,
        Bounce,
        Use,
        PickUp,

        Hurt,
        Dead,
        HealUp,
    }
}
