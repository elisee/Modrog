using SDL2;
using SwarmBasics;
using SwarmBasics.Math;
using SwarmPlatform.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SwarmPlatform.UI
{
    public class Element
    {
        public readonly Desktop Desktop;
        public Element Parent;
        public readonly List<Element> Children = new List<Element>();

        public bool Visible
        {
            get => _visible;
            set
            {
                _visible = value;

                if (_visible != IsMounted)
                {
                    if (IsMounted) Unmount();
                    else if (Parent.IsMounted) Mount();
                }
            }
        }

        bool _visible = true;

        public bool Disabled
        {
            get => _disabled;
            set
            {
                _disabled = value;

                if (_disabled)
                {
                    if (Desktop.FocusedElement.IsContainedWithin(this)) Desktop.SetFocusedElement(Desktop.RootElement);
                    if (Desktop.HoveredElement.IsContainedWithin(this)) Desktop.RefreshHoveredElement(clearPressed: true);
                }
            }
        }

        bool _disabled;

        public int LayoutWeight = 0;

        // Self layout
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
        public Rectangle ViewRectangle { get; private set; }

        protected Rectangle _contentRectangle { get; private set; }
        protected Point _contentScroll;
        protected int _scrollMultiplier = 30;
        public int ScrollbarThickness = 8;

        // Child layout
        public enum ChildLayoutMode { Stack, Overlay, Left, Right, Top, Bottom }
        public ChildLayoutMode ChildLayout = ChildLayoutMode.Stack;

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
            if (IsMounted && child.Visible) child.Mount();
        }

        public void Remove(Element child)
        {
            Debug.Assert(child.Parent == this);

            if (child.IsMounted) child.Unmount();
            child.Parent = null;
            Children.Remove(child);
        }

        bool IsContainedWithin(Element element) => element == this || (element.Parent != null && IsContainedWithin(element.Parent));

        public void ComputeSizes(int? maxWidth, int? maxHeight, out Point layoutSize, out Point paddedContentSize)
        {
            layoutSize = new Point(
                (Width ?? 0) + (Left ?? 0) + (Right ?? 0),
                (Height ?? 0) + (Top ?? 0) + (Bottom ?? 0));

            var contentSize = ComputeContentSize(maxWidth, maxHeight);

            paddedContentSize = new Point(
                contentSize.X + LeftPadding + RightPadding,
                contentSize.Y + TopPadding + BottomPadding);

            if (Width == null && HorizontalFlow != Flow.Scroll) layoutSize.X += paddedContentSize.X;
            if (Height == null && VerticalFlow != Flow.Scroll) layoutSize.Y += paddedContentSize.Y;
        }

        protected virtual Point ComputeContentSize(int? maxWidth, int? maxHeight)
        {
            var contentSize = Point.Zero;

            var contentMaxWidth = Width ?? maxWidth;
            if (contentMaxWidth != null) contentMaxWidth -= LeftPadding + RightPadding;

            var contentMaxHeight = Height ?? maxHeight;
            if (contentMaxHeight != null) contentMaxHeight -= TopPadding + BottomPadding;

            switch (ChildLayout)
            {
                case ChildLayoutMode.Stack:
                case ChildLayoutMode.Overlay:
                    foreach (var child in Children)
                    {
                        if (!child.IsMounted) continue;
                        child.ComputeSizes(contentMaxWidth, contentMaxHeight, out var childLayoutSize, out _);
                        if (Width == null || HorizontalFlow == Flow.Scroll) contentSize.X = Math.Max(contentSize.X, childLayoutSize.X);
                        if (Height == null || VerticalFlow == Flow.Scroll) contentSize.Y = Math.Max(contentSize.Y, childLayoutSize.Y);
                    }
                    break;

                case ChildLayoutMode.Left:
                case ChildLayoutMode.Right:
                    foreach (var child in Children)
                    {
                        if (!child.IsMounted) continue;
                        child.ComputeSizes(contentMaxWidth, contentMaxHeight, out var childLayoutSize, out _);
                        if (Width == null || HorizontalFlow == Flow.Scroll) contentSize.X += childLayoutSize.X;
                        if (Height == null || VerticalFlow == Flow.Scroll) contentSize.Y = Math.Max(contentSize.Y, childLayoutSize.Y);
                    }
                    break;

                case ChildLayoutMode.Top:
                case ChildLayoutMode.Bottom:
                    foreach (var child in Children)
                    {
                        if (!child.IsMounted) continue;
                        child.ComputeSizes(contentMaxWidth, contentMaxHeight, out var childLayoutSize, out _);
                        if (Width == null || HorizontalFlow == Flow.Scroll) contentSize.X = Math.Max(contentSize.X, childLayoutSize.X);
                        if (Height == null || VerticalFlow == Flow.Scroll) contentSize.Y += childLayoutSize.Y;
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (contentMaxWidth != null && contentSize.X > contentMaxWidth && HorizontalFlow != Flow.Scroll) contentSize.X = contentMaxWidth.Value;
            if (contentMaxHeight != null && contentSize.Y > contentMaxHeight && VerticalFlow != Flow.Scroll) contentSize.Y = contentMaxHeight.Value;

            return contentSize;
        }

        Point GetScrollArea() => new Point(
            HorizontalFlow == Flow.Scroll ? _contentRectangle.Width - ViewRectangle.Width : 0,
            VerticalFlow == Flow.Scroll ? _contentRectangle.Height - ViewRectangle.Height : 0);

        public void Layout(Rectangle? containerRectangle = null)
        {
            if (!IsMounted) return;

            if (containerRectangle != null) _containerRectangle = containerRectangle.Value;
            else if (_containerRectangle == Rectangle.Zero) return;

            ComputeSizes(_containerRectangle.Width, _containerRectangle.Height, out var minLayoutSize, out var minPaddedContentSize);

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
                    layoutRectangle.X += _containerRectangle.Width / 2 - minLayoutSize.X / 2;
                    layoutRectangle.Width = minLayoutSize.X;
                }

                if (Left != null)
                {
                    layoutRectangle.X += Left.Value;
                    layoutRectangle.Width -= Left.Value;
                    if (Right == null && HorizontalFlow == Flow.Shrink) layoutRectangle.X = _containerRectangle.X + Left.Value;
                }

                if (Right != null)
                {
                    layoutRectangle.Width -= Right.Value;
                    if (Left == 0 && HorizontalFlow == Flow.Shrink) layoutRectangle.X = _containerRectangle.Right - Right.Value - layoutRectangle.Width;
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
                    layoutRectangle.Y += _containerRectangle.Height / 2 - minLayoutSize.Y / 2;
                    layoutRectangle.Height = minLayoutSize.Y;
                }

                if (Top != null)
                {
                    layoutRectangle.Y += Top.Value;
                    layoutRectangle.Height -= Top.Value;
                    if (Bottom == null && VerticalFlow == Flow.Shrink) layoutRectangle.Y = _containerRectangle.Y + Top.Value;
                }

                if (Bottom != null)
                {
                    layoutRectangle.Height -= Bottom.Value;
                    if (Top == null && VerticalFlow == Flow.Shrink) layoutRectangle.Y = _containerRectangle.Bottom - Bottom.Value - layoutRectangle.Height;
                }
            }

            LayoutRectangle = layoutRectangle;

            ViewRectangle = new Rectangle(LayoutRectangle.X + LeftPadding, LayoutRectangle.Y + TopPadding,
                LayoutRectangle.Width - LeftPadding - RightPadding,
                LayoutRectangle.Height - TopPadding - BottomPadding);

            _contentRectangle = new Rectangle(
                LayoutRectangle.X + LeftPadding, LayoutRectangle.Y + TopPadding,
                Math.Max(minPaddedContentSize.X, LayoutRectangle.Width) - LeftPadding - RightPadding,
                Math.Max(minPaddedContentSize.Y, LayoutRectangle.Height) - TopPadding - BottomPadding) - _contentScroll;

            Debug.Assert(minPaddedContentSize.X <= LayoutRectangle.Width || HorizontalFlow == Flow.Scroll);
            Debug.Assert(minPaddedContentSize.Y <= LayoutRectangle.Height || VerticalFlow == Flow.Scroll);

            LayoutSelf();

            var maxWidth = _contentRectangle.Width;
            var maxHeight = _contentRectangle.Height;

            var fixedSize = 0;
            var fixedSizes = new int[Children.Count];
            var flexWeights = 0;

            var secondarySize = 0;

            switch (ChildLayout)
            {
                case ChildLayoutMode.Stack:
                case ChildLayoutMode.Overlay:
                    foreach (var child in Children) if (child.IsMounted) child.Layout(_contentRectangle);
                    break;

                case ChildLayoutMode.Left:
                case ChildLayoutMode.Right:
                    {
                        for (var i = 0; i < Children.Count; i++)
                        {
                            var child = Children[i];
                            if (!child.IsMounted) continue;

                            child.ComputeSizes(maxWidth, maxHeight, out var childSize, out _);
                            secondarySize = Math.Max(secondarySize, childSize.Y);

                            if (child.LayoutWeight == 0 || HorizontalFlow != Flow.Expand) fixedSize += (fixedSizes[i] = childSize.X);
                            else flexWeights += child.LayoutWeight;
                        }

                        var flexSize = _contentRectangle.Width - fixedSize;
                        var offset = 0;

                        for (var i = 0; i < Children.Count; i++)
                        {
                            var child = Children[i];
                            if (!child.IsMounted) continue;

                            var size = (child.LayoutWeight == 0 || HorizontalFlow != Flow.Expand) ? fixedSizes[i] : flexSize * child.LayoutWeight / flexWeights;
                            child.Layout(new Rectangle(_contentRectangle.X + offset, _contentRectangle.Y, size, _contentRectangle.Height));
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

                            child.ComputeSizes(maxWidth, maxHeight, out var childSize, out _);
                            secondarySize = Math.Max(secondarySize, childSize.X);

                            if (child.LayoutWeight == 0 || VerticalFlow != Flow.Expand) fixedSize += (fixedSizes[i] = childSize.Y);
                            else flexWeights += child.LayoutWeight;
                        }

                        var flexSize = _contentRectangle.Height - fixedSize;
                        var offset = 0;

                        for (var i = 0; i < Children.Count; i++)
                        {
                            var child = Children[i];
                            if (!child.IsMounted) continue;

                            var size = (child.LayoutWeight == 0 || VerticalFlow != Flow.Expand) ? fixedSizes[i] : flexSize * child.LayoutWeight / flexWeights;
                            child.Layout(new Rectangle(_contentRectangle.X, _contentRectangle.Y + offset, _contentRectangle.Width, size));
                            offset += size;
                        }
                        break;
                    }

                default:
                    throw new NotSupportedException();
            }

            // Clamp scroll
            ScrollIntoView(null, null);
        }

        public virtual void LayoutSelf() { }

        public virtual Element HitTest(int x, int y)
        {
            if (!LayoutRectangle.Contains(x, y) || Disabled) return null;

            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var child = Children[i];
                if (!child.IsMounted) continue;

                var hitElement = child.HitTest(x, y);
                if (hitElement != null) return hitElement;

                if (ChildLayout == ChildLayoutMode.Stack) break;
            }

            return null;
        }

        internal void Mount()
        {
            Debug.Assert(!IsMounted);
            IsMounted = true;

            foreach (var child in Children) if (child.Visible) child.Mount();

            OnMounted();
        }

        internal void Unmount()
        {
            Debug.Assert(IsMounted);
            IsMounted = false;

            if (IsFocused) Desktop.SetFocusedElement(Desktop.RootElement);
            if (IsHovered) Desktop.RefreshHoveredElement(clearPressed: true);

            foreach (var child in Children) if (child.IsMounted) child.Unmount();

            OnUnmounted();
        }

        public virtual void OnMounted() { }
        public virtual void OnUnmounted() { }

        public virtual bool AcceptsFocus() => false;

        public virtual void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            if (!repeat && Desktop.HasNoKeyModifier)
            {
                if (key == SDL.SDL_Keycode.SDLK_RETURN || key == SDL.SDL_Keycode.SDLK_KP_ENTER) { Validate(); return; }
                else if (key == SDL.SDL_Keycode.SDLK_ESCAPE) { Dismiss(); return; }
            }

            if (key == SDL.SDL_Keycode.SDLK_TAB && (Desktop.HasNoKeyModifier || Desktop.IsShiftOnlyDown))
            {
                Desktop.MoveFocus(backwards: Desktop.IsShiftOnlyDown);
                return;
            }

            Parent?.OnKeyDown(key, repeat);
        }

        public virtual void OnKeyUp(SDL.SDL_Keycode key) { Parent?.OnKeyUp(key); }
        public virtual void OnTextEntered(string text) { }

        public virtual void OnBlur() { }
        public virtual void OnFocus() { }

        public virtual void OnMouseEnter() { }
        public virtual void OnMouseExit() { }
        public virtual void OnMouseMove() { }

        public virtual void OnMouseDown(int button, int clicks) { }
        public virtual void OnMouseUp(int button) { }
        public virtual void OnMouseWheel(int dx, int dy)
        {
            var scrollArea = GetScrollArea();

            var newContentScroll = new Point(
                Math.Clamp(_contentScroll.X + dx * _scrollMultiplier, 0, scrollArea.X),
                Math.Clamp(_contentScroll.Y - dy * _scrollMultiplier, 0, scrollArea.Y));

            // Allow scrolling horizontally with vertical mouse wheel in the proper conditions
            if (dx == 0 && dy != 0 && HorizontalFlow == Flow.Scroll && VerticalFlow != Flow.Scroll)
            {
                newContentScroll = new Point(Math.Clamp(_contentScroll.X - dy * _scrollMultiplier, 0, scrollArea.X), 0);
            }

            var scrollOffset = newContentScroll - _contentScroll;

            if (scrollOffset == Point.Zero)
            {
                Parent?.OnMouseWheel(dx, dy);
                return;
            }

            foreach (var child in Children) child.ApplyParentScroll(scrollOffset);
            _contentScroll = newContentScroll;
            _contentRectangle -= scrollOffset;
        }

        public virtual void Validate() => Parent?.Validate();
        public virtual void Dismiss() => Parent?.Dismiss();

        public void ScrollIntoView(int? x, int? y)
        {
            var scrollRectangle = new Rectangle(
                _contentScroll.X, _contentScroll.Y,
                ViewRectangle.Width, ViewRectangle.Height);

            if (x != null)
            {
                if (x.Value < scrollRectangle.X) scrollRectangle.X = x.Value;
                else if (x.Value > scrollRectangle.Right) scrollRectangle.X = x.Value - scrollRectangle.Width;
            }

            if (y != null)
            {
                if (y.Value < scrollRectangle.Y) scrollRectangle.Y = y.Value;
                else if (y.Value > scrollRectangle.Bottom) scrollRectangle.Y = y.Value - scrollRectangle.Height;
            }

            var scrollArea = GetScrollArea();
            var newContentScroll = new Point(Math.Clamp(scrollRectangle.X, 0, scrollArea.X), Math.Clamp(scrollRectangle.Y, 0, scrollArea.Y));

            if (newContentScroll != _contentScroll)
            {
                var scrollOffset = newContentScroll - _contentScroll;
                foreach (var child in Children) child.ApplyParentScroll(scrollOffset);

                _contentRectangle -= scrollOffset;
                _contentScroll = newContentScroll;
            }
        }

        public void ScrollToBottom() => ScrollIntoView(null, _contentRectangle.Height);

        protected void ApplyParentScroll(Point offset)
        {
            if (!IsMounted) return;

            LayoutRectangle -= offset;
            ViewRectangle -= offset;
            _contentRectangle -= offset;

            foreach (var child in Children) child.ApplyParentScroll(offset);
        }

        public void Draw()
        {
            Debug.Assert(IsMounted);

            DrawSelf();

            if (Children.Count > 0)
            {
                var shouldClip = HorizontalFlow == Flow.Scroll || VerticalFlow == Flow.Scroll;

                if (shouldClip) Desktop.PushClipRect(ViewRectangle);
                foreach (var child in Children) if (child.IsMounted) child.Draw();
                if (shouldClip) Desktop.PopClipRect();
            }

            var hasHorizontalScrollbar = HorizontalFlow == Flow.Scroll && ViewRectangle.Width < _contentRectangle.Width;
            var hasVerticalScrollbar = VerticalFlow == Flow.Scroll && ViewRectangle.Height < _contentRectangle.Height;

            SDL.SDL_SetRenderDrawBlendMode(Desktop.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
            new Color(0xffffff60).UseAsDrawColor(Desktop.Renderer);

            if (hasVerticalScrollbar)
            {
                var trackHeight = LayoutRectangle.Height - (hasHorizontalScrollbar ? ScrollbarThickness : 0);
                var thumbHeight = Math.Max(ScrollbarThickness, trackHeight * ViewRectangle.Height / _contentRectangle.Height);
                var travel = trackHeight - thumbHeight;
                var y = travel * _contentScroll.Y / (_contentRectangle.Height - ViewRectangle.Height);

                var verticalRect = new SDL.SDL_Rect { x = LayoutRectangle.Right - ScrollbarThickness, y = LayoutRectangle.Y + y, w = ScrollbarThickness, h = thumbHeight };
                SDL.SDL_RenderFillRect(Desktop.Renderer, ref verticalRect);
            }

            if (hasHorizontalScrollbar)
            {
                var trackWidth = LayoutRectangle.Width - (hasVerticalScrollbar ? ScrollbarThickness : 0);
                var thumbWidth = Math.Max(ScrollbarThickness, trackWidth * ViewRectangle.Width / _contentRectangle.Width);
                var travel = trackWidth - thumbWidth;
                var x = travel * _contentScroll.X / (_contentRectangle.Width - ViewRectangle.Width);

                var verticalRect = new SDL.SDL_Rect { x = LayoutRectangle.X + x, y = LayoutRectangle.Bottom - ScrollbarThickness, w = thumbWidth, h = ScrollbarThickness };
                SDL.SDL_RenderFillRect(Desktop.Renderer, ref verticalRect);
            }

            SDL.SDL_SetRenderDrawBlendMode(Desktop.Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);
            new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);
        }

        public void DrawOutline()
        {
            Debug.Assert(IsMounted);

            var rect = new Rectangle(LayoutRectangle.X, LayoutRectangle.Y, LayoutRectangle.Width, LayoutRectangle.Height).ToSDL_Rect();
            SDL.SDL_RenderDrawRect(Desktop.Renderer, ref rect);
        }

        protected virtual void DrawSelf()
        {
            BackgroundPatch?.Draw(Desktop.Renderer, LayoutRectangle);
        }
    }
}
