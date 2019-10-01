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

        public enum ChildLayoutMode { Overlay, Left, Right, Top, Bottom }
        public ChildLayoutMode ChildLayout = ChildLayoutMode.Overlay;

        public int LayoutWeight = 0;

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

        public virtual Point ComputeSize(int? maxWidth, int? maxHeight)
        {
            var size = new Point(
                (Anchor.Width ?? 0) + (Anchor.Left ?? 0) + (Anchor.Right ?? 0),
                (Anchor.Height ?? 0) + (Anchor.Top ?? 0) + (Anchor.Bottom ?? 0));

            if (Anchor.Width == null || Anchor.Height == null)
            {
                var contentSize = Point.Zero;
                var childMaxWidth = Anchor.Width ?? maxWidth;
                var childMaxHeight = Anchor.Height ?? maxHeight;

                switch (ChildLayout)
                {
                    case ChildLayoutMode.Overlay:
                        foreach (var child in Children)
                        {
                            if (!child.IsMounted) continue;
                            var childSize = child.ComputeSize(childMaxWidth, childMaxHeight);
                            if (Anchor.Width == null) contentSize.X = Math.Max(contentSize.X, childSize.X);
                            if (Anchor.Height == null) contentSize.Y = Math.Max(contentSize.Y, childSize.Y);
                        }
                        break;

                    case ChildLayoutMode.Left:
                    case ChildLayoutMode.Right:
                        foreach (var child in Children)
                        {
                            if (!child.IsMounted) continue;
                            var childSize = child.ComputeSize(childMaxWidth, childMaxHeight);
                            if (Anchor.Width == null) contentSize.X += childSize.X;
                            if (Anchor.Height == null) contentSize.Y = Math.Max(contentSize.Y, childSize.Y);
                        }
                        break;

                    case ChildLayoutMode.Top:
                    case ChildLayoutMode.Bottom:
                        foreach (var child in Children)
                        {
                            if (!child.IsMounted) continue;
                            var childSize = child.ComputeSize(childMaxWidth, childMaxHeight);
                            if (Anchor.Width == null) contentSize.X = Math.Max(contentSize.X, childSize.X);
                            if (Anchor.Height == null) contentSize.Y += childSize.Y;
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }

                if (Anchor.Width == null) size.X += contentSize.X;
                if (Anchor.Height == null) size.Y += contentSize.Y;
            }

            return size;
        }

        public void Layout(Rectangle container)
        {
            var minSize = ComputeSize(container.Width, container.Height);

            LayoutRectangle = container;

            if (Anchor.Width != null)
            {
                LayoutRectangle.Width = Anchor.Width.Value;

                if (Anchor.Left != null) LayoutRectangle.X += Anchor.Left.Value;
                else if (Anchor.Right != null) LayoutRectangle.X = container.X + container.Width - Anchor.Right.Value - Anchor.Width.Value;
                else LayoutRectangle.X += container.Width / 2 - LayoutRectangle.Width / 2;
            }
            else
            {
                if (Anchor.HorizontalFlow == Flow.Shrink)
                {
                    LayoutRectangle.X += container.Width / 2 - minSize.X / 2;
                    LayoutRectangle.Width = minSize.X;
                }

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
                else LayoutRectangle.Y += container.Height / 2 - LayoutRectangle.Height / 2;
            }
            else
            {
                if (Anchor.VerticalFlow == Flow.Shrink)
                {
                    LayoutRectangle.Y += container.Height / 2 - minSize.Y / 2;
                    LayoutRectangle.Height = minSize.Y;
                }

                if (Anchor.Top != null)
                {
                    LayoutRectangle.Y += Anchor.Top.Value;
                    LayoutRectangle.Height -= Anchor.Top.Value;
                }

                if (Anchor.Bottom != null) LayoutRectangle.Height -= Anchor.Bottom.Value;
            }

            LayoutSelf();

            var maxWidth = LayoutRectangle.Width;
            var maxHeight = LayoutRectangle.Height;

            var fixedSize = 0;
            var fixedSizes = new int[Children.Count];
            var flexWeights = 0;

            var secondarySize = 0;

            switch (ChildLayout)
            {
                case ChildLayoutMode.Overlay:
                    foreach (var child in Children) if (child.IsMounted) child.Layout(LayoutRectangle);
                    break;

                case ChildLayoutMode.Left:
                case ChildLayoutMode.Right:
                    {
                        for (var i = 0; i < Children.Count; i++)
                        {
                            var child = Children[i];
                            if (!child.IsMounted) continue;

                            var childSize = child.ComputeSize(maxWidth, maxHeight);
                            secondarySize = Math.Max(secondarySize, childSize.Y);

                            if (child.LayoutWeight == 0 || Anchor.HorizontalFlow != Flow.Expand) fixedSize += (fixedSizes[i] = childSize.X);
                            else flexWeights += child.LayoutWeight;
                        }

                        var flexSize = LayoutRectangle.Width - fixedSize;
                        var offset = 0;

                        for (var i = 0; i < Children.Count; i++)
                        {
                            var child = Children[i];
                            if (!child.IsMounted) continue;

                            var size = (child.LayoutWeight == 0 || Anchor.HorizontalFlow != Flow.Expand) ? fixedSizes[i] : flexSize * child.LayoutWeight / flexWeights;
                            child.Layout(new Rectangle(LayoutRectangle.X + offset, LayoutRectangle.Y, size, LayoutRectangle.Height));
                            offset += size;
                        }
                        break;
                    }

                case ChildLayoutMode.Top:
                case ChildLayoutMode.Bottom:
                    {
                        for (var i = 0; i < Children.Count; i++)
                        {
                            var child = Children[i];
                            if (!child.IsMounted) continue;

                            var childSize = child.ComputeSize(maxWidth, maxHeight);
                            secondarySize = Math.Max(secondarySize, childSize.X);

                            if (child.LayoutWeight == 0 || Anchor.VerticalFlow != Flow.Expand) fixedSize += (fixedSizes[i] = childSize.Y);
                            else flexWeights += child.LayoutWeight;
                        }

                        var flexSize = LayoutRectangle.Height - fixedSize;
                        var offset = 0;

                        // TODO: Use Width to compute LayoutRectangle, shouldn't be computed before here I think

                        for (var i = 0; i < Children.Count; i++)
                        {
                            var child = Children[i];
                            if (!child.IsMounted) continue;

                            var size = (child.LayoutWeight == 0 || Anchor.VerticalFlow != Flow.Expand) ? fixedSizes[i] : flexSize * child.LayoutWeight / flexWeights;
                            child.Layout(new Rectangle(LayoutRectangle.X, LayoutRectangle.Y + offset, LayoutRectangle.Width, size));
                            offset += size;
                        }
                        break;
                    }

                default:
                    throw new NotSupportedException();
            }
        }

        public virtual void LayoutSelf() { }

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
