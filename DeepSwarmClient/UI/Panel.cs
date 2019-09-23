namespace DeepSwarmClient.UI
{
    class Panel : Element
    {
        public Panel(Desktop desktop, Element parent, Color backgroundColor)
            : base(desktop, parent)
        {
            BackgroundColor = backgroundColor;
        }

        public override Element HitTest(int x, int y) => base.HitTest(x, y) ?? (LayoutRectangle.Contains(x, y) ? this : null);
    }
}
