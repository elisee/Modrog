using DeepSwarmBasics.Math;
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

        // Anchoring
        public int? Left;
        public int? Right;
        public int? Top;
        public int? Bottom;

        public int? Width;
        public int? Height;

        public Flow Flow { set => HorizontalFlow = VerticalFlow = value; }
        public Flow HorizontalFlow;
        public Flow VerticalFlow;

        public int LeftPadding;
        public int RightPadding;
        public int TopPadding;
        public int BottomPadding;

        public int Padding { set => LeftPadding = RightPadding = TopPadding = BottomPadding = value; }
        public int HorizontalPadding { set => LeftPadding = RightPadding = value; }
        public int VerticalPadding { set => TopPadding = BottomPadding = value; }

        Rectangle _containerRectangle;
        public Rectangle LayoutRectangle { get; private set; }
        public Rectangle RectangleAfterPadding { get; private set; }

        // Child layout
        public enum ChildLayoutMode { Overlay, Left, Right, Top, Bottom }
        public ChildLayoutMode ChildLayout = ChildLayoutMode.Overlay;

        // Background
        public TexturePatch BackgroundPatch;
        public Color OutlineColor;

        // State
        public bool IsMounted { get; private set; }
        public bool IsFocused => Desktop.FocusedElement == this;
        public bool IsHovered => Desktop.HoveredElement == this;
        public bool IsPressed => Desktop.HoveredElement == this && Desktop.IsHoveredElementPressed;

        public Element(Element parent) : this(parent.Desktop, parent) { }
        public Element(Desktop desktop, Element parent = null)
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
                (Width ?? 0) + (Left ?? 0) + (Right ?? 0),
                (Height ?? 0) + (Top ?? 0) + (Bottom ?? 0));

            if (Width == null || Height == null)
            {
                var contentSize = Point.Zero;
                var childMaxWidth = Width ?? maxWidth;
                var childMaxHeight = Height ?? maxHeight;

                switch (ChildLayout)
                {
                    case ChildLayoutMode.Overlay:
                        foreach (var child in Children)
                        {
                            if (!child.IsMounted) continue;
                            var childSize = child.ComputeSize(childMaxWidth, childMaxHeight);
                            if (Width == null) contentSize.X = Math.Max(contentSize.X, childSize.X);
                            if (Height == null) contentSize.Y = Math.Max(contentSize.Y, childSize.Y);
                        }
                        break;

                    case ChildLayoutMode.Left:
                    case ChildLayoutMode.Right:
                        foreach (var child in Children)
                        {
                            if (!child.IsMounted) continue;
                            var childSize = child.ComputeSize(childMaxWidth, childMaxHeight);
                            if (Width == null) contentSize.X += childSize.X;
                            if (Height == null) contentSize.Y = Math.Max(contentSize.Y, childSize.Y);
                        }
                        break;

                    case ChildLayoutMode.Top:
                    case ChildLayoutMode.Bottom:
                        foreach (var child in Children)
                        {
                            if (!child.IsMounted) continue;
                            var childSize = child.ComputeSize(childMaxWidth, childMaxHeight);
                            if (Width == null) contentSize.X = Math.Max(contentSize.X, childSize.X);
                            if (Height == null) contentSize.Y += childSize.Y;
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }

                if (Width == null) size.X += contentSize.X + LeftPadding + RightPadding;
                if (Height == null) size.Y += contentSize.Y + TopPadding + BottomPadding;
            }

            return size;
        }

        public void Layout(Rectangle? containerRectangle = null)
        {
            if (!IsMounted) return;

            if (containerRectangle != null) _containerRectangle = containerRectangle.Value;

            var minSize = ComputeSize(_containerRectangle.Width, _containerRectangle.Height);

            var layoutRectangle = _containerRectangle;

            if (Width != null)
            {
                layoutRectangle.Width = Width.Value;

                if (Left != null) layoutRectangle.X += Left.Value;
                else if (Right != null) layoutRectangle.X = _containerRectangle.Right - Width.Value - Right.Value;
                else layoutRectangle.X += _containerRectangle.Width / 2 - layoutRectangle.Width / 2;
            }
            else
            {
                if (HorizontalFlow == Flow.Shrink)
                {
                    layoutRectangle.X += _containerRectangle.Width / 2 - minSize.X / 2;
                    layoutRectangle.Width = minSize.X;
                }

                if (Left != null)
                {
                    layoutRectangle.X += Left.Value;
                    layoutRectangle.Width -= Left.Value;
                    if (Right == null && HorizontalFlow == Flow.Shrink) layoutRectangle.X = _containerRectangle.X;
                }

                if (Right != null)
                {
                    layoutRectangle.Width -= Right.Value;
                    if (Left == 0 && HorizontalFlow == Flow.Shrink) layoutRectangle.X = _containerRectangle.Right - layoutRectangle.Width;
                }
            }

            if (Height != null)
            {
                layoutRectangle.Height = Height.Value;

                if (Top != null) layoutRectangle.Y += Top.Value;
                else if (Bottom != null) layoutRectangle.Y = _containerRectangle.Bottom - Height.Value - Bottom.Value;
                else layoutRectangle.Y += _containerRectangle.Height / 2 - layoutRectangle.Height / 2;
            }
            else
            {
                if (VerticalFlow == Flow.Shrink)
                {
                    layoutRectangle.Y += _containerRectangle.Height / 2 - minSize.Y / 2;
                    layoutRectangle.Height = minSize.Y;
                }

                if (Top != null)
                {
                    layoutRectangle.Y += Top.Value;
                    layoutRectangle.Height -= Top.Value;
                    if (Bottom == null && VerticalFlow == Flow.Shrink) layoutRectangle.Y = _containerRectangle.Y;
                }

                if (Bottom != null)
                {
                    layoutRectangle.Height -= Bottom.Value;
                    if (Top == null && VerticalFlow == Flow.Shrink) layoutRectangle.Y = _containerRectangle.Bottom - layoutRectangle.Height;
                }
            }

            LayoutRectangle = layoutRectangle;
            RectangleAfterPadding = new Rectangle(LayoutRectangle.X + LeftPadding, LayoutRectangle.Y + TopPadding, LayoutRectangle.Width - LeftPadding - RightPadding, LayoutRectangle.Height - TopPadding - BottomPadding);

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

                            if (child.LayoutWeight == 0 || HorizontalFlow != Flow.Expand) fixedSize += (fixedSizes[i] = childSize.X);
                            else flexWeights += child.LayoutWeight;
                        }

                        var flexSize = RectangleAfterPadding.Width - fixedSize;
                        var offset = 0;

                        for (var i = 0; i < Children.Count; i++)
                        {
                            var child = Children[i];
                            if (!child.IsMounted) continue;

                            var size = (child.LayoutWeight == 0 || HorizontalFlow != Flow.Expand) ? fixedSizes[i] : flexSize * child.LayoutWeight / flexWeights;
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

                            if (child.LayoutWeight == 0 || VerticalFlow != Flow.Expand) fixedSize += (fixedSizes[i] = childSize.Y);
                            else flexWeights += child.LayoutWeight;
                        }

                        var flexSize = RectangleAfterPadding.Height - fixedSize;
                        var offset = 0;

                        for (var i = 0; i < Children.Count; i++)
                        {
                            var child = Children[i];
                            if (!child.IsMounted) continue;

                            var size = (child.LayoutWeight == 0 || VerticalFlow != Flow.Expand) ? fixedSizes[i] : flexSize * child.LayoutWeight / flexWeights;
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

        public virtual bool AcceptsFocus() => false;

        public virtual void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            if (!repeat)
            {
                if (key == SDL.SDL_Keycode.SDLK_RETURN) Validate();
                else if (key == SDL.SDL_Keycode.SDLK_ESCAPE) Dismiss();
                else if (key == SDL.SDL_Keycode.SDLK_TAB) Desktop.MoveFocus(backwards: Desktop.IsShiftDown);
            }
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

        public void DrawOutline()
        {
            Debug.Assert(IsMounted);

            SDL.SDL_RenderDrawLine(Desktop.Renderer, LayoutRectangle.Left, LayoutRectangle.Top, LayoutRectangle.Right, LayoutRectangle.Top);
            SDL.SDL_RenderDrawLine(Desktop.Renderer, LayoutRectangle.Right, LayoutRectangle.Top, LayoutRectangle.Right, LayoutRectangle.Bottom);
            SDL.SDL_RenderDrawLine(Desktop.Renderer, LayoutRectangle.Right, LayoutRectangle.Bottom, LayoutRectangle.Left, LayoutRectangle.Bottom);
            SDL.SDL_RenderDrawLine(Desktop.Renderer, LayoutRectangle.Left, LayoutRectangle.Bottom, LayoutRectangle.Left, LayoutRectangle.Top);
        }

        protected virtual void DrawSelf()
        {
            if (BackgroundPatch != null) RendererHelper.DrawPatch(Desktop.Renderer, BackgroundPatch, LayoutRectangle);
        }
    }
}
