using SDL2;

namespace DeepSwarmClient.UI
{
    struct Rectangle
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Contains(int x, int y)
        {
            return x >= X && y >= Y && x < X + Width && y < Y + Height;
        }

        public SDL.SDL_Rect ToSDL_Rect() => new SDL.SDL_Rect { x = X, y = Y, w = Width, h = Height };
    }
}
