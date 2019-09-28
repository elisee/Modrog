using DeepSwarmCommon;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DeepSwarmClient.UI
{
    class Element
    {
        public readonly Desktop Desktop;
        public Element Parent;
        public readonly List<Element> Children = new List<Element>();

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;

                if (_isVisible != IsMounted)
                {
                    if (IsMounted) Unmount();
                    else if (Parent.IsMounted) Mount();
                }
            }
        }

        bool _isVisible = true;

        public Anchor Anchor;
        public Color BackgroundColor;

        public IntPtr BackgroundTexture;
        public Rectangle BackgroundTextureArea;

        public Rectangle LayoutRectangle;

        public bool IsMounted { get; private set; }
        public bool IsFocused => Desktop.FocusedElement == this;
        public bool IsHovered => Desktop.HoveredElement == this;
        public bool IsPressed => Desktop.HoveredElement == this && Desktop.IsHoveredElementPressed;

        public Element(Desktop desktop, Element parent)
        {
            Desktop = desktop;
            parent?.Add(this);
        }

        public void Clear()
        {
            foreach (var child in Children)
            {
                if (child.IsMounted) child.Unmount();
                child.Parent = null;
            }
            Children.Clear();
        }

        public void Add(Element child)
        {
            Debug.Assert(child.Parent == null);

            Children.Add(child);
            child.Parent = this;
            if (IsMounted && child.IsVisible) child.Mount();
        }

        public void Remove(Element child)
        {
            Debug.Assert(child.Parent == this);

            if (child.IsMounted) child.Unmount();
            child.Parent = null;
            Children.Remove(child);
        }

        public void Layout(Rectangle container)
        {
            LayoutRectangle = container;

            if (Anchor.Width != null)
            {
                LayoutRectangle.Width = Anchor.Width.Value;

                if (Anchor.Left != null) LayoutRectangle.X += Anchor.Left.Value;
                else if (Anchor.Right != null) LayoutRectangle.X = container.X + container.Width - Anchor.Right.Value - Anchor.Width.Value;
                else LayoutRectangle.X = container.X + container.Width / 2 - LayoutRectangle.Width / 2;
            }
            else
            {
                if (Anchor.Left != null)
                {
                    LayoutRectangle.X += Anchor.Left.Value;
                    LayoutRectangle.Width -= Anchor.Left.Value;
                }

                if (Anchor.Right != null) LayoutRectangle.Width -= Anchor.Right.Value;
            }

            if (Anchor.Height != null)
            {
                LayoutRectangle.Height = Anchor.Height.Value;

                if (Anchor.Top != null) LayoutRectangle.Y += Anchor.Top.Value;
                else if (Anchor.Bottom != null) LayoutRectangle.Y = container.Y + container.Height - Anchor.Bottom.Value - Anchor.Height.Value;
                else LayoutRectangle.Y = container.Y + container.Height / 2 - LayoutRectangle.Height / 2;
            }
            else
            {
                if (Anchor.Top != null)
                {
                    LayoutRectangle.Y += Anchor.Top.Value;
                    LayoutRectangle.Height -= Anchor.Top.Value;
                }

                if (Anchor.Bottom != null) LayoutRectangle.Height -= Anchor.Bottom.Value;
            }

            foreach (var child in Children) if (child.IsMounted) child.Layout(LayoutRectangle);
        }

        public virtual Element HitTest(int x, int y)
        {
            if (!LayoutRectangle.Contains(x, y)) return null;

            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var child = Children[i];
                if (!child.IsMounted) continue;

                var hitElement = child.HitTest(x, y);
                if (hitElement != null) return hitElement;
            }

            return null;
        }

        internal void Mount()
        {
            Debug.Assert(!IsMounted);
            IsMounted = true;

            foreach (var child in Children) if (child.IsVisible) child.Mount();

            OnMounted();
        }

        internal void Unmount()
        {
            Debug.Assert(IsMounted);
            IsMounted = false;

            if (IsFocused) Desktop.SetFocusedElement(Desktop.RootElement);
            if (IsHovered) Desktop.OnHoveredElementUnmounted();

            foreach (var child in Children) if (child.IsMounted) child.Unmount();

            OnUnmounted();
        }

        public virtual void OnMounted() { }
        public virtual void OnUnmounted() { }

        public virtual void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            if (!repeat && key == SDL.SDL_Keycode.SDLK_RETURN) Validate();
            else if (!repeat && key == SDL.SDL_Keycode.SDLK_ESCAPE) Dismiss();
        }

        public virtual void OnKeyUp(SDL.SDL_Keycode key) { }
        public virtual void OnTextEntered(string text) { }

        public virtual void OnBlur() { }
        public virtual void OnFocus() { }

        public virtual void OnMouseEnter() { }
        public virtual void OnMouseExit() { }
        public virtual void OnMouseMove() { }

        public virtual void OnMouseDown(int button) { }
        public virtual void OnMouseUp(int button) { }
        public virtual void OnMouseWheel(int dx, int dy) { }

        public virtual void Validate() => Parent?.Validate();
        public virtual void Dismiss() => Parent?.Dismiss();

        public void Draw()
        {
            Debug.Assert(IsMounted);

            DrawSelf();
            foreach (var child in Children) if (child.IsMounted) child.Draw();
        }

        protected virtual void DrawSelf()
        {
            if (BackgroundColor.A != 0)
            {
                if (BackgroundColor.A != byte.MaxValue) SDL.SDL_SetRenderDrawBlendMode(Desktop.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
                var rect = Desktop.ToSDL_Rect(LayoutRectangle);
                BackgroundColor.UseAsDrawColor(Desktop.Renderer);
                SDL.SDL_RenderFillRect(Desktop.Renderer, ref rect);
                if (BackgroundColor.A != byte.MaxValue) SDL.SDL_SetRenderDrawBlendMode(Desktop.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);
            }

            if (BackgroundTexture != IntPtr.Zero)
            {
                var sourceRect = Desktop.ToSDL_Rect(BackgroundTextureArea);
                var destRect = Desktop.ToSDL_Rect(LayoutRectangle);
                SDL.SDL_RenderCopy(Desktop.Renderer, BackgroundTexture, ref sourceRect, ref destRect);
            }
        }
    }
}
