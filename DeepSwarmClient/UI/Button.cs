using System;

namespace DeepSwarmClient.UI
{
    class Button : Element
    {
        public Color TextColor = new Color(0xffffffff);
        public Color PressedTextColor = new Color(0xff0000ff);
        public string Text = "";

        bool _isPressed;

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
                _isPressed = true;
            }
        }

        public override void OnMouseUp(int button)
        {
            if (button == 1)
            {
                _isPressed = false;
                OnActivate?.Invoke();
            }
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            RendererHelper.DrawText(Desktop.Renderer, _layoutRectangle.X, _layoutRectangle.Y, Text, _isPressed ? PressedTextColor : TextColor);
        }
    }
}
