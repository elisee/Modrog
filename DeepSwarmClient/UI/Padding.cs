namespace DeepSwarmClient.UI
{
    public struct Padding
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;

        public int All { set => Left = Right = Top = Bottom = value; }
        public int Horizontal { get => Left + Right; set => Left = Right = value; }
        public int Vertical { get => Top + Bottom; set => Top = Bottom = value; }
    }
}
