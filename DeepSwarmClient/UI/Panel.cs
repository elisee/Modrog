namespace DeepSwarmClient.UI
{
    public class Panel : Element
    {
        public Panel(Desktop desktop, Element parent, TexturePatch backgroundPatch)
            : base(desktop, parent)
        {
            BackgroundPatch = backgroundPatch;
        }

        public override Element HitTest(int x, int y) => base.HitTest(x, y) ?? (LayoutRectangle.Contains(x, y) ? this : null);
    }
}
