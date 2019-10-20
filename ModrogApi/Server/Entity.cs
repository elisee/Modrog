﻿using SwarmBasics.Math;

namespace ModrogApi.Server
{
    public abstract class Entity
    {
        public abstract World GetWorld();
        public abstract Point GetPosition();

        public abstract void Teleport(World world, Point position, EntityDirection direction);
    }
}