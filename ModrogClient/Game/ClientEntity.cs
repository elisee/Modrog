using ModrogApi;
using SwarmBasics.Math;
using System;

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

        public ClientEntity(int id, Point position)
        {
            Id = id;
            Position = PreviousTickPosition = position;
        }

        public void ApplyTickAction(EntityAction action)
        {
            PreviousTickPosition = Position;
            Action = action;

            switch (action)
            {
                case EntityAction.MoveRight: Position = new Point(Position.X + 1, Position.Y); break;
                case EntityAction.MoveDown: Position = new Point(Position.X, Position.Y + 1); break;
                case EntityAction.MoveLeft: Position = new Point(Position.X - 1, Position.Y); break;
                case EntityAction.MoveUp: Position = new Point(Position.X, Position.Y - 1); break;
                default: throw new NotImplementedException();
            }
        }
    }
}
