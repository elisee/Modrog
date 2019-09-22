using System;

namespace DeepSwarmClient.UI
{
    class Button : Element
    {
        public Color TextColor = new Color(0xffffffff);
        public string Text = "";

        public Action OnActivate;

        public Button(Desktop desktop, Element parent)
             : base(desktop, parent)
        {
        }

        public override Element HitTest(int x, int y) => _layoutRectangle.Contains(x, y) ? this : null;

        public override void OnMouseDown(int button)
        {
            if (button == 1)
            {
                OnActivate?.Invoke();
            }
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            TextColor.UseAsDrawColor(Desktop.Renderer);
            RendererHelper.DrawText(Desktop.Renderer, _layoutRectangle.X, _layoutRectangle.Y, Text);
        }
    }
}
