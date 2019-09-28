using SDL2;
using System;

namespace DeepSwarmClient.UI
{
    class TextInput : Element
    {
        public Color TextColor = new Color(0xffffffff);
        public string Value { get; private set; } = "";
        public int MaxLength = byte.MaxValue;

        int _cursorX;
        float _cursorTimer;
        public const float CursorFlashInterval = 1f;

        int _scrollingPixelsX;

        public TextInput(Desktop desktop, Element parent)
            : base(desktop, parent)
        {
        }

        #region Configuration
        public void SetValue(string value)
        {
            Value = value;
            _cursorX = value.Length;
        }
        #endregion

        #region Internals
        void Animate(float deltaTime)
        {
            _cursorTimer += deltaTime;
        }

        void ClampScrolling()
        {
            _scrollingPixelsX = Math.Clamp(_scrollingPixelsX,
                Math.Max(0, _cursorX * RendererHelper.FontRenderSize - LayoutRectangle.Width),
                Math.Max(0, Math.Min(_cursorX * RendererHelper.FontRenderSize, Value.Length * RendererHelper.FontRenderSize - LayoutRectangle.Width)));
        }
        #endregion

        #region Events
        public override Element HitTest(int x, int y) => LayoutRectangle.Contains(x, y) ? this : null;

        public override void OnMouseEnter() => SDL.SDL_SetCursor(RendererHelper.IbeamCursor);
        public override void OnMouseExit() => SDL.SDL_SetCursor(RendererHelper.ArrowCursor);

        public override void OnFocus() { _cursorTimer = 0f; Desktop.RegisterAnimation(Animate); }
        public override void OnBlur() => Desktop.UnregisterAnimation(Animate);

        public override void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_LEFT: GoLeft(); break;
                case SDL.SDL_Keycode.SDLK_RIGHT: GoRight(); break;
                case SDL.SDL_Keycode.SDLK_BACKSPACE: Erase(); break;
                case SDL.SDL_Keycode.SDLK_DELETE: Delete(); break;
                default: base.OnKeyDown(key, repeat); break;
            }

            void GoLeft()
            {
                if (_cursorX > 0)
                {
                    _cursorX--;
                    ClampScrolling();
                }

                _cursorTimer = 0f;
            }

            void GoRight()
            {
                if (_cursorX < Value.Length)
                {
                    _cursorX++;
                    ClampScrolling();
                }

                _cursorTimer = 0f;
            }

            void Erase()
            {
                if (_cursorX > 0)
                {
                    Value = Value[0..(_cursorX - 1)] + Value[_cursorX..];
                    _cursorX--;
                    ClampScrolling();
                }

                _cursorTimer = 0f;
            }

            void Delete()
            {
                if (_cursorX < Value.Length)
                {
                    Value = Value[0.._cursorX] + Value[(_cursorX + 1)..];
                    ClampScrolling();
                }

                _cursorTimer = 0f;
            }
        }

        public override void OnTextEntered(string text)
        {
            if (Value.Length >= MaxLength) return;
            if (Value.Length + text.Length > MaxLength) text = text[0..(MaxLength - Value.Length)];

            Value = Value[0.._cursorX] + text + Value[_cursorX..];
            _cursorX += text.Length;
            _cursorTimer = 0f;
            ClampScrolling();
        }

        public override void OnMouseDown(int button)
        {
            if (button == 1)
            {
                Desktop.SetFocusedElement(this);
                var x = Desktop.MouseX + _scrollingPixelsX - LayoutRectangle.X;

                int targetX = x / RendererHelper.FontRenderSize;

                bool wasRightHalfClicked = x % RendererHelper.FontRenderSize > RendererHelper.FontRenderSize / 2;
                if (wasRightHalfClicked) targetX++;

                _cursorX = Math.Clamp(targetX, 0, Value.Length);
            }
        }
        #endregion

        #region Drawing
        protected override void DrawSelf()
        {
            base.DrawSelf();

            var clipRect = Desktop.ToSDL_Rect(LayoutRectangle);
            SDL.SDL_RenderSetClipRect(Desktop.Renderer, ref clipRect);

            RendererHelper.DrawText(Desktop.Renderer, LayoutRectangle.X - _scrollingPixelsX, LayoutRectangle.Y, Value, TextColor);

            SDL.SDL_RenderSetClipRect(Desktop.Renderer, IntPtr.Zero);

            if (Desktop.FocusedElement == this && (_cursorTimer % CursorFlashInterval * 2) < CursorFlashInterval)
            {
                new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);

                var cursorRect = Desktop.ToSDL_Rect(new DeepSwarmCommon.Rectangle(
                    LayoutRectangle.X + _cursorX * RendererHelper.FontRenderSize - _scrollingPixelsX,
                    LayoutRectangle.Y,
                    2, LayoutRectangle.Height));
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref cursorRect);
            }
        }
        #endregion
    }
}
