using DeepSwarmCommon;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DeepSwarmClient.UI
{
    public class Element
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

        public int LayoutWeight = 0;

        public Anchor Anchor;
        public Padding Padding;

        public enum ChildLayoutMode { Overlay, Left, Right, Top, Bottom }
        public ChildLayoutMode ChildLayout = ChildLayoutMode.Overlay;

        public TexturePatch BackgroundPatch;

        Rectangle _containerRectangle;
        public Rectangle LayoutRectangle { get; private set; }
        public Rectangle RectangleAfterPadding { get; private set; }

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

                if (Anchor.Width == null) size.X += contentSize.X + Padding.Horizontal;
                if (Anchor.Height == null) size.Y += contentSize.Y + Padding.Vertical;
            }

            return size;
        }

        public void Layout(Rectangle? containerRectangle = null)
        {
            if (containerRectangle != null) _containerRectangle = containerRectangle.Value;

            var minSize = ComputeSize(_containerRectangle.Width, _containerRectangle.Height);

            var layoutRectangle = _containerRectangle;

            if (Anchor.Width != null)
            {
                layoutRectangle.Width = Anchor.Width.Value;

                if (Anchor.Left != null) layoutRectangle.X += Anchor.Left.Value;
                else if (Anchor.Right != null) layoutRectangle.X = _containerRectangle.Right - Anchor.Width.Value - Anchor.Right.Value;
                else layoutRectangle.X += _containerRectangle.Width / 2 - layoutRectangle.Width / 2;
            }
            else
            {
                if (Anchor.HorizontalFlow == Flow.Shrink)
                {
                    layoutRectangle.X += _containerRectangle.Width / 2 - minSize.X / 2;
                    layoutRectangle.Width = minSize.X;
                }

                if (Anchor.Left != null)
                {
                    layoutRectangle.X += Anchor.Left.Value;
                    layoutRectangle.Width -= Anchor.Left.Value;
                }

                if (Anchor.Right != null) layoutRectangle.Width -= Anchor.Right.Value;
            }

            if (Anchor.Height != null)
            {
                layoutRectangle.Height = Anchor.Height.Value;

                if (Anchor.Top != null) layoutRectangle.Y += Anchor.Top.Value;
                else if (Anchor.Bottom != null) layoutRectangle.Y = _containerRectangle.Bottom - Anchor.Height.Value - Anchor.Bottom.Value;
                else layoutRectangle.Y += _containerRectangle.Height / 2 - layoutRectangle.Height / 2;
            }
            else
            {
                if (Anchor.VerticalFlow == Flow.Shrink)
                {
                    layoutRectangle.Y += _containerRectangle.Height / 2 - minSize.Y / 2;
                    layoutRectangle.Height = minSize.Y;
                }

                if (Anchor.Top != null)
                {
                    layoutRectangle.Y += Anchor.Top.Value;
                    layoutRectangle.Height -= Anchor.Top.Value;
                }

                if (Anchor.Bottom != null) layoutRectangle.Height -= Anchor.Bottom.Value;
            }

            LayoutRectangle = layoutRectangle;
            RectangleAfterPadding = new Rectangle(LayoutRectangle.X + Padding.Left, LayoutRectangle.Y + Padding.Top, LayoutRectangle.Width - Padding.Horizontal, LayoutRectangle.Height - Padding.Vertical);

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
                    foreach (var child in Children) if (child.IsMounted) child.Layout(RectangleAfterPadding);
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

                        var flexSize = RectangleAfterPadding.Width - fixedSize;
                        var offset = 0;

                        for (var i = 0; i < Children.Count; i++)
                        {
                            var child = Children[i];
                            if (!child.IsMounted) continue;

                            var size = (child.LayoutWeight == 0 || Anchor.HorizontalFlow != Flow.Expand) ? fixedSizes[i] : flexSize * child.LayoutWeight / flexWeights;
                            child.Layout(new Rectangle(RectangleAfterPadding.X + offset, RectangleAfterPadding.Y, size, RectangleAfterPadding.Height));
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

                        var flexSize = RectangleAfterPadding.Height - fixedSize;
                        var offset = 0;

                        for (var i = 0; i < Children.Count; i++)
                        {
                            var child = Children[i];
                            if (!child.IsMounted) continue;

                            var size = (child.LayoutWeight == 0 || Anchor.VerticalFlow != Flow.Expand) ? fixedSizes[i] : flexSize * child.LayoutWeight / flexWeights;
                            child.Layout(new Rectangle(RectangleAfterPadding.X, RectangleAfterPadding.Y + offset, RectangleAfterPadding.Width, size));
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
            if (BackgroundPatch != null) RendererHelper.DrawPatch(Desktop.Renderer, BackgroundPatch, LayoutRectangle);
        }
    }
}
