namespace DeepSwarmClient.UI
{
    public enum Flow { Expand, Shrink, Scroll }

    public struct Anchor
    {
        public int? Left;
        public int? Right;
        public int? Top;
        public int? Bottom;

        public int? Width;
        public int? Height;

        public Flow Flow { set { HorizontalFlow = VerticalFlow = value; } }
        public Flow HorizontalFlow;
        public Flow VerticalFlow;

        // TODO: Remove this constructor, can just use {}
        public Anchor(int? left = null, int? right = null, int? top = null, int? bottom = null, int? width = null, int? height = null)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
            Width = width;
            Height = height;
            HorizontalFlow = Flow.Expand;
            VerticalFlow = Flow.Expand;
        }
    }
}
