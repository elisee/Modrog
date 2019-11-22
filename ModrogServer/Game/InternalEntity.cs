using ModrogApi;
using ModrogApi.Server;
using ModrogCommon;
using SwarmBasics.Math;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ModrogServer.Game
{
    sealed class InternalEntity : Entity, IEquatable<InternalEntity>
    {
        internal readonly int Id;
        internal InternalWorld World;
        internal Point Position;

        internal readonly InternalCharacterKind CharacterKind;
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
        internal bool AreItemSlotsDirty;

        internal readonly InternalItemKind ItemKind;

        public InternalEntity(int id, InternalWorld world, Point position, CharacterKind characterKind, int playerIndex)
        {
            Id = id;
            Position = PreviousTickPosition = position;
            CharacterKind = (InternalCharacterKind)characterKind;

            PlayerIndex = playerIndex;
            if (PlayerIndex != -1)
            {
                world.Universe.Players[PlayerIndex].OwnedEntities.Add(this);
                world.Universe.Players[PlayerIndex].OwnedEntitiesById.Add(Id, this);
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
            World.Remove(this);

            Position = PreviousTickPosition = Point.Zero;
            Action = EntityAction.Idle;
            ActionDirection = Direction.Down;
            ActionItem = null;
        }

        public override ItemKind GetItem(int slot) => ItemSlots[slot];
        public override void SetItem(int slot, ItemKind itemKind)
        {
            ItemSlots[slot] = (InternalItemKind)itemKind;
            AreItemSlotsDirty = true;
        }
        #endregion
    }
}
