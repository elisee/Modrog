﻿using DeepSwarmBasics;
using DeepSwarmBasics.Math;
using DeepSwarmPlatform.Graphics;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DeepSwarmPlatform.UI
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
        protected Rectangle _viewRectangle { get; private set; }

        protected Rectangle _contentRectangle { get; private set; }
        Point _contentScroll;
        public const int ScrollMultiplier = 30;

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

        public virtual void ComputeSizes(int? maxWidth, int? maxHeight, out Point layoutSize, out Point paddedContentSize)
        {
            layoutSize = new Point(
                (Width ?? 0) + (Left ?? 0) + (Right ?? 0),
                (Height ?? 0) + (Top ?? 0) + (Bottom ?? 0));

            var contentSize = Point.Zero;

            var contentMaxWidth = Width ?? maxWidth;
            if (contentMaxWidth != null) contentMaxWidth -= LeftPadding + RightPadding;

            var contentMaxHeight = Height ?? maxHeight;
            if (contentMaxHeight != null) contentMaxHeight -= TopPadding + BottomPadding;

            switch (ChildLayout)
            {
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

            paddedContentSize = new Point(
                contentSize.X + LeftPadding + RightPadding,
                contentSize.Y + TopPadding + BottomPadding);

            if (Width == null && HorizontalFlow != Flow.Scroll) layoutSize.X += paddedContentSize.X;
            if (Height == null && VerticalFlow != Flow.Scroll) layoutSize.Y += paddedContentSize.Y;
        }

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
                    layoutRectangle.Y += _containerRectangle.Height / 2 - minLayoutSize.Y / 2;
                    layoutRectangle.Height = minLayoutSize.Y;
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

            _viewRectangle = new Rectangle(LayoutRectangle.X + LeftPadding, LayoutRectangle.Y + TopPadding,
                LayoutRectangle.Width - LeftPadding - RightPadding,
                LayoutRectangle.Height - TopPadding - BottomPadding);

            _contentRectangle = new Rectangle(
                LayoutRectangle.X + LeftPadding, LayoutRectangle.Y + TopPadding,
                Math.Max(minPaddedContentSize.X, LayoutRectangle.Width) - LeftPadding - RightPadding,
                Math.Max(minPaddedContentSize.Y, LayoutRectangle.Height) - TopPadding - BottomPadding) - _contentScroll;

            Debug.Assert(minPaddedContentSize.X <= LayoutRectangle.Width || HorizontalFlow == Flow.Scroll);
            Debug.Assert(minPaddedContentSize.Y <= LayoutRectangle.Height || VerticalFlow == Flow.Scroll);

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
                if (key == SDL.SDL_Keycode.SDLK_RETURN || key == SDL.SDL_Keycode.SDLK_KP_ENTER) Validate();
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
        public virtual void OnMouseWheel(int dx, int dy)
        {
            var scrollArea = new Point(_contentRectangle.Width - _viewRectangle.Width, _contentRectangle.Height - _viewRectangle.Height);

            var newContentScroll = new Point(
                Math.Clamp(_contentScroll.X - dx * ScrollMultiplier, 0, scrollArea.X),
                Math.Clamp(_contentScroll.Y - dy * ScrollMultiplier, 0, scrollArea.Y));

            var scrollOffset = newContentScroll - _contentScroll;

            if (scrollOffset == Point.Zero)
            {
                Parent?.OnMouseWheel(dx, dy);
                return;
            }

            foreach (var child in Children) child.ApplyParentScroll(scrollOffset);
            _contentScroll = newContentScroll;
        }

        public virtual void Validate() => Parent?.Validate();
        public virtual void Dismiss() => Parent?.Dismiss();

        protected void ApplyParentScroll(Point offset)
        {
            LayoutRectangle -= offset;
            _viewRectangle -= offset;
            _contentRectangle -= offset;

            foreach (var child in Children) child.ApplyParentScroll(offset);
        }

        public void Draw()
        {
            Debug.Assert(IsMounted);

            DrawSelf();

            if (HorizontalFlow == Flow.Scroll || VerticalFlow == Flow.Scroll) Desktop.PushClipRect(_viewRectangle);

            foreach (var child in Children) if (child.IsMounted) child.Draw();

            if (HorizontalFlow == Flow.Scroll || VerticalFlow == Flow.Scroll) Desktop.PopClipRect();
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
            BackgroundPatch?.Draw(Desktop.Renderer, LayoutRectangle);
        }
    }
}
