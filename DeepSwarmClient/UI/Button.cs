﻿using DeepSwarmCommon;
using System;

namespace DeepSwarmClient.UI
{
    public class Button : Element
    {
        public TexturePatch HoveredPatch;
        public TexturePatch PressedPatch;

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

        public override Point ComputeSize(int? maxWidth, int? maxHeight)
        {
            return base.ComputeSize(maxWidth, maxHeight);
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            if (IsHovered) { if (HoveredPatch != null) RendererHelper.DrawPatch(Desktop.Renderer, HoveredPatch, LayoutRectangle); }
            else if (IsPressed) { if (PressedPatch != null) RendererHelper.DrawPatch(Desktop.Renderer, PressedPatch, LayoutRectangle); }
        }
    }
}
