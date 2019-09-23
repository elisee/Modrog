using System;

namespace DeepSwarmClient.UI
{
    class Button : Element
    {
        public Color TextColor = new Color(0xffffffff);
        public Color HoveredTextColor = new Color(0xaaff66ff);
        public Color PressedTextColor = new Color(0xff0000ff);
        public string Text = "";

        bool _isHovered;
        bool _isPressed;

        public Action OnActivate;

        public Button(Desktop desktop, Element parent)
             : base(desktop, parent)
        {
        }

        public override Element HitTest(int x, int y) => LayoutRectangle.Contains(x, y) ? this : null;

        public override void OnMouseEnter() => _isHovered = true;
        public override void OnMouseExit() => _isHovered = false;

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

            RendererHelper.DrawText(Desktop.Renderer, LayoutRectangle.X, LayoutRectangle.Y, Text, _isPressed ? PressedTextColor : (_isHovered ? HoveredTextColor : TextColor));
        }
    }
}
