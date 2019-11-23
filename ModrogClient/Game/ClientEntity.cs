using ModrogApi;
using ModrogCommon;
using SwarmBasics.Math;

namespace ModrogClient.Game
{
    class ClientEntity
    {
        public readonly int Id;
        public Point Position { get; private set; }

        public readonly ClientCharacterKind CharacterKind;
        public int Health;
        public ClientItemKind[] ItemSlots = new ClientItemKind[Protocol.CharacterItemSlotCount];
        public int PlayerIndex = -1;

        public Point PreviousTickPosition { get; private set; }
        public EntityAction Action { get; private set; }
        public Direction ActionDirection { get; private set; }
        public ClientItemKind ActionItem { get; private set; }

        public readonly ClientItemKind ItemKind;

        public ClientEntity(int id, Point position, ClientCharacterKind characterKind, int health, int playerIndex)
        {
            Id = id;
            Position = PreviousTickPosition = position;
            CharacterKind = characterKind;
            Health = health;
            PlayerIndex = playerIndex;
        }

        public ClientEntity(int id, Point position, ClientItemKind itemKind)
        {
            Id = id;
            Position = PreviousTickPosition = position;
            ItemKind = itemKind;
        }

        public void PreTick()
        {
            PreviousTickPosition = Position;
            Action = EntityAction.Idle;
            ActionItem = null;
        }

        public void ApplyTeleportAction(Point position, Direction direction)
        {
            Action = EntityAction.Teleport;
            PreviousTickPosition = Position = position;
            ActionDirection = direction;
        }

        public void ApplyMoveAction(Direction direction)
        {
            Action = EntityAction.Move;
            ActionDirection = direction;

            switch (ActionDirection)
            {
                case Direction.Right: Position = new Point(Position.X + 1, Position.Y); break;
                case Direction.Down: Position = new Point(Position.X, Position.Y + 1); break;
                case Direction.Left: Position = new Point(Position.X - 1, Position.Y); break;
                case Direction.Up: Position = new Point(Position.X, Position.Y - 1); break;
            }
        }

        public void ApplyBounceAction(Direction direction)
        {
            Action = EntityAction.Bounce;
            ActionDirection = direction;
        }

        public void ApplyUseAction(Direction direction, ClientItemKind itemKind)
        {
            Action = EntityAction.Use;
            ActionDirection = direction;
            ActionItem = itemKind;
        }
    }
}
