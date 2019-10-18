using DeepSwarmBasics;
using DeepSwarmPlatform.Graphics;
using System;

namespace DeepSwarmPlatform.UI
{
    public class Button : Element
    {
        public TexturePatch HoveredPatch = new TexturePatch(0xff0000ff);
        public TexturePatch PressedPatch = new TexturePatch(0x0000ffff);

        public Action OnActivate;

        public Button(Element parent) : this(parent.Desktop, parent) { }

        public Button(Desktop desktop, Element parent) : base(desktop, parent)
        {
            OutlineColor = new Color(0xff0000ff);
        }

        public override Element HitTest(int x, int y) => LayoutRectangle.Contains(x, y) ? this : null;

        public override bool AcceptsFocus() => !Disabled;

        public override void OnMouseEnter()
        {
            SDL2.SDL.SDL_SetCursor(Cursors.HandCursor);
        }

        public override void OnMouseExit()
        {
            SDL2.SDL.SDL_SetCursor(Cursors.ArrowCursor);
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

        public override void Validate() => OnActivate?.Invoke();

        protected override void DrawSelf()
        {
            base.DrawSelf();

            if (IsPressed) PressedPatch?.Draw(Desktop.Renderer, LayoutRectangle);
            else if (IsHovered) HoveredPatch?.Draw(Desktop.Renderer, LayoutRectangle);
        }
    }
}
