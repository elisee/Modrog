namespace DeepSwarmClient.UI
{
    class Label : Element
    {
        public Color TextColor = new Color(0xffffffff);
        public string Text = "";

        public Label(Desktop desktop, Element parent)
             : base(desktop, parent)
        {
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            RendererHelper.DrawText(Desktop.Renderer, LayoutRectangle.X, LayoutRectangle.Y, Text, TextColor);
        }
    }
}
