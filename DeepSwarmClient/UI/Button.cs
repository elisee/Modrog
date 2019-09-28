using System;

namespace DeepSwarmClient.UI
{
    class Button : Element
    {
        public Color TextColor = new Color(0xffffffff);
        public Color HoveredTextColor = new Color(0xaaff66ff);
        public Color PressedTextColor = new Color(0xff0000ff);
        public string Text = "";

        public Action OnActivate;

        public Button(Desktop desktop, Element parent)
             : base(desktop, parent)
        {
        }

        public override Element HitTest(int x, int y) => LayoutRectangle.Contains(x, y) ? this : null;

        public override void OnMouseEnter()
        {
            SDL2.SDL.SDL_SetCursor(RendererHelper.HandCursor);
        }

        public override void OnMouseExit()
        {
            SDL2.SDL.SDL_SetCursor(RendererHelper.ArrowCursor);
        }

        public override void OnMouseDown(int button)
        {
            if (button == 1)
            {
                Desktop.SetFocusedElement(this);
                Desktop.SetHoveredElementPressed(true);
            }
        }

        public override void OnMouseUp(int button)
        {
            if (button == 1 && IsPressed)
            {
                Desktop.SetHoveredElementPressed(false);
                if (IsHovered) OnActivate?.Invoke();
            }
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            RendererHelper.DrawText(Desktop.Renderer, LayoutRectangle.X, LayoutRectangle.Y, Text, IsPressed ? PressedTextColor : (IsHovered ? HoveredTextColor : TextColor));
        }
    }
}
