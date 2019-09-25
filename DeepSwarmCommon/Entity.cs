using System;

namespace DeepSwarmCommon
{
    public class Entity
    {
        public enum EntityType
        {
            Factory,
            Heart,
            Robot,
            /* SmallMonster,
            MediumMonster,
            BigMonster */
        }

        public struct EntityStats
        {
            public uint BlueColor;
            public uint RedColor;
            public uint NeutralColor;

            public int InitialHealth;
            public int InitialCrystals;

            public int BuildPrice;

            public int OmniViewRadius;
            public int DirectionalViewRadius;
            public float HalfFieldOfView;
        }

        public static readonly EntityStats[] EntityStatsByType = new EntityStats[]
        {
            new EntityStats { BlueColor = 0x443b3aff, RedColor = 0x443b3aff, InitialHealth = 100, InitialCrystals = 100, OmniViewRadius = 8 },
            new EntityStats { BlueColor = 0x1d4ddfff, RedColor = 0xcc1414ff, InitialHealth = 50, OmniViewRadius = 8 },
            new EntityStats { BlueColor = 0x6879e4ff, RedColor = 0xd36565ff, InitialHealth = 10, BuildPrice = 50, NeutralColor = 0x807d7cff, OmniViewRadius = 2, DirectionalViewRadius = 8, HalfFieldOfView = MathF.PI / 3f },
        };

        public enum EntityDirection { Right, Down, Left, Up }
        public enum EntityMove { Idle, RotateCW, RotateCCW, Forward, Attack, PickUp, Use, Build }

        public int Id;
        public EntityType Type;
        public int PlayerIndex = -1;
        public int X;
        public int Y;

        public int Health;
        public int Crystals;
        public EntityDirection Direction;
        public EntityMove UpcomingMove;

        public float GetDirectionAngle() => Direction switch
        {
            EntityDirection.Right => 0f,
            EntityDirection.Down => MathF.PI / 2f,
            EntityDirection.Left => MathF.PI,
            EntityDirection.Up => MathF.PI * 3f / 2f,
            _ => throw new NotSupportedException(),
        };

        public EntityMove GetMoveForTargetDirection(EntityDirection targetDirection)
        {
            if (targetDirection == Direction) return Entity.EntityMove.Forward;
            else
            {
                var diff = (Direction - targetDirection + 4) % 4 - 2;
                if (diff < 0) return Entity.EntityMove.RotateCCW;
                else return Entity.EntityMove.RotateCW;
            }
        }
    }
}
