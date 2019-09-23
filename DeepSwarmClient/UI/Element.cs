using DeepSwarmCommon;
using SDL2;
using System.Collections.Generic;
using System.Diagnostics;

namespace DeepSwarmClient.UI
{
    class Element
    {
        public readonly Desktop Desktop;
        public Element Parent;
        public readonly List<Element> Children = new List<Element>();

        public Rectangle AnchorRectangle;
        public Color BackgroundColor;

        protected Rectangle _layoutRectangle;

        public Element(Desktop desktop, Element parent)
        {
            Desktop = desktop;
            Parent = parent;
            if (parent != null) parent.Children.Add(this);
        }

        public void Clear()
        {
            foreach (var child in Children) child.Parent = null;
            Children.Clear();
        }

        public void Add(Element child)
        {
            Debug.Assert(child.Parent == null);

            Children.Add(child);
            child.Parent = this;
        }

        public void Remove(Element child)
        {
            Debug.Assert(child.Parent == this);

            child.Parent = null;
            Children.Remove(child);
        }

        public void Layout(Rectangle container)
        {
            _layoutRectangle = new Rectangle(
                container.X + AnchorRectangle.X,
                container.Y + AnchorRectangle.Y,
                AnchorRectangle.Width, AnchorRectangle.Height);

            foreach (var child in Children) child.Layout(_layoutRectangle);
        }

        public virtual Element HitTest(int x, int y)
        {
            if (!_layoutRectangle.Contains(x, y)) return null;

            foreach (var child in Children)
            {
                var hitElement = child.HitTest(x, y);
                if (hitElement != null) return hitElement;
            }

            return null;
        }

        public virtual void OnKeyDown(SDL.SDL_Keycode key, bool repeat) { }
        public virtual void OnKeyUp(SDL.SDL_Keycode key) { }
        public virtual void OnMouseDown(int button) { }
        public virtual void OnMouseUp(int button) { }
        public virtual void OnTextEntered(string text) { }

        public void Draw()
        {
            DrawSelf();
            foreach (var child in Children) child.Draw();
        }

        protected virtual void DrawSelf()
        {
            if (BackgroundColor.A != 0)
            {
                var rect = Desktop.ToSDL_Rect(_layoutRectangle);
                BackgroundColor.UseAsDrawColor(Desktop.Renderer);
                SDL.SDL_RenderFillRect(Desktop.Renderer, ref rect);
            }
        }
    }
}
