﻿using SDL2;

namespace DeepSwarmClient.UI
{
    class TextInput : Element
    {
        public Color TextColor = new Color(0xffffffff);
        public string Value = "";
        public int MaxLength = 0;

        public TextInput(Desktop desktop, Element parent)
            : base(desktop, parent)
        {
        }

        public override Element HitTest(int x, int y) => LayoutRectangle.Contains(x, y) ? this : null;

        public override void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_BACKSPACE:
                    if (Value.Length > 0) Value = Value[0..^1];
                    break;
                default:
                    base.OnKeyDown(key, repeat);
                    break;
            }
        }

        public override void OnTextEntered(string text)
        {
            if (Value.Length >= MaxLength) return;
            if (Value.Length + text.Length > MaxLength) text = text.Substring(0, MaxLength - Value.Length);

            Value += text;
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            RendererHelper.DrawText(Desktop.Renderer, LayoutRectangle.X, LayoutRectangle.Y, Value, TextColor);

            if (Desktop.FocusedElement == this)
            {
                new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);
                SDL.SDL_RenderDrawLine(Desktop.Renderer,
                    LayoutRectangle.X + Value.Length * 16, LayoutRectangle.Y,
                    LayoutRectangle.X + Value.Length * 16, LayoutRectangle.Y + LayoutRectangle.Height);
            }
        }
    }
}
