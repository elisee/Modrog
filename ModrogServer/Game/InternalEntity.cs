using ModrogApi;
using ModrogApi.Server;
using ModrogCommon;
using SwarmBasics.Math;
using System;
using System.Diagnostics.CodeAnalysis;
using static ModrogCommon.Protocol;

namespace ModrogServer.Game
{
    sealed class InternalEntity : Entity, IEquatable<InternalEntity>
    {
        internal readonly int Id;
        internal InternalWorld World;
        internal Point Position;
        internal EntityDirtyFlags DirtyFlags;

        internal readonly InternalCharacterKind CharacterKind;
        internal int Health;
        internal readonly InternalItemKind[] ItemSlots = new InternalItemKind[Protocol.CharacterItemSlotCount];
        internal int PlayerIndex;
        public int ViewRadius = 2;
        internal Point PreviousTickPosition;

        internal CharacterIntent Intent = CharacterIntent.Idle;
        internal Direction IntentDirection;
        internal int IntentSlot;
        internal EntityAction Action = EntityAction.Idle;
        internal Direction ActionDirection;
        internal InternalItemKind ActionItem;

        internal readonly InternalItemKind ItemKind;

        public InternalEntity(int id, InternalWorld world, Point position, CharacterKind characterKind, int playerIndex)
        {
            Id = id;
            Position = PreviousTickPosition = position;
            CharacterKind = (InternalCharacterKind)characterKind;
            Health = CharacterKind.Health;

            PlayerIndex = playerIndex;
            if (PlayerIndex != -1)
            {
                var player = world.Universe.Players[PlayerIndex];
                player.OwnedEntities.Add(this);
                player.OwnedEntitiesById.Add(Id, this);
            }

            world.Add(this);
        }

        public InternalEntity(int id, InternalWorld world, Point position, ItemKind itemKind)
        {
            Id = id;
            Position = position;
            ItemKind = (InternalItemKind)itemKind;

            world.Add(this);
        }

        public override bool Equals(object obj) => Equals(obj as InternalEntity);
        public bool Equals([AllowNull] InternalEntity other) => other != null && Id == other.Id;
        public override int GetHashCode() => HashCode.Combine(Id);

        public override void SetView(int omniViewRadius)
        {
            ViewRadius = omniViewRadius;
        }

        #region API
        public override World GetWorld() => World;
        public override Point GetPosition() => Position;

        public override CharacterKind GetCharacterKind() => CharacterKind;
        public override ItemKind GetItemKind() => ItemKind;

        public override void Teleport(World world, Point position)
        {
            World.Remove(this);
            ((InternalWorld)world).Add(this);

            Position = PreviousTickPosition = position;
            Action = EntityAction.Idle;
            ActionDirection = Direction.Down;
            ActionItem = null;
        }

        public override void Remove()
        {
            if (PlayerIndex != -1)
            {
                var player = World.Universe.Players[PlayerIndex];
                player.OwnedEntities.Remove(this);
                player.OwnedEntitiesById.Remove(Id);
            }

            World.Remove(this);
            World = null;

            Position = PreviousTickPosition = Point.Zero;
            Action = EntityAction.Idle;
            ActionDirection = Direction.Down;
            ActionItem = null;
        }

        public override int GetHealth() => Health;
        public override void SetHealth(int health)
        {
            Health = health;
            DirtyFlags |= EntityDirtyFlags.Health;
        }

        public override ItemKind GetItem(int slot) => ItemSlots[slot];
        public override void SetItem(int slot, ItemKind itemKind)
        {
            ItemSlots[slot] = (InternalItemKind)itemKind;
            DirtyFlags |= EntityDirtyFlags.ItemSlots;
        }
        #endregion
    }
}
