﻿using DeepSwarmCommon;
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

        public Rectangle AnchorRectangle;
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
            LayoutRectangle = new Rectangle(
                container.X + AnchorRectangle.X,
                container.Y + AnchorRectangle.Y,
                AnchorRectangle.Width, AnchorRectangle.Height);

            foreach (var child in Children) if (child.IsMounted) child.Layout(LayoutRectangle);
        }

        public virtual Element HitTest(int x, int y)
        {
            if (!LayoutRectangle.Contains(x, y)) return null;

            foreach (var child in Children)
            {
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
                var rect = Desktop.ToSDL_Rect(LayoutRectangle);
                BackgroundColor.UseAsDrawColor(Desktop.Renderer);
                SDL.SDL_RenderFillRect(Desktop.Renderer, ref rect);
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
