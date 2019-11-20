using ModrogApi;
using SwarmBasics.Math;

namespace ModrogClient.Game
{
    public class ClientEntity
    {
        public readonly int Id;
        public Point SpriteLocation;
        public int PlayerIndex;

        public Point PreviousTickPosition { get; private set; }
        public Point Position { get; private set; }
        public EntityAction Action { get; private set; }
        public Direction ActionDirection { get; private set; }
        // TODO: public ClientItemKind ActionItem { get; private set; }

        public ClientEntity(int id, Point position)
        {
            Id = id;
            Position = PreviousTickPosition = position;
        }

        public void ClearPreviousTick()
        {
            PreviousTickPosition = Position;
            Action = EntityAction.Idle;
        }

        public void ApplyTick(EntityAction action, Direction actionDirection) // TODO: , ClientItemKind actionItem)
        {
            PreviousTickPosition = Position;
            Action = action;
            ActionDirection = actionDirection;
            // TODO: ActionItem = actionItem;

            switch (action)
            {
                case EntityAction.Move:
                    switch (actionDirection)
                    {
                        case Direction.Right: Position = new Point(Position.X + 1, Position.Y); break;
                        case Direction.Down: Position = new Point(Position.X, Position.Y + 1); break;
                        case Direction.Left: Position = new Point(Position.X - 1, Position.Y); break;
                        case Direction.Up: Position = new Point(Position.X, Position.Y - 1); break;
                    }
                    break;

                default:
                    // Ignore
                    break;
            }
        }
    }
}
