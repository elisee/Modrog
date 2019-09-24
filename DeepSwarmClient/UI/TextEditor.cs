using SDL2;
using System;
using System.Collections.Generic;

namespace DeepSwarmClient.UI
{
    class TextEditor : Element
    {
        public Color TextColor = new Color(0xffffffff);

        public readonly List<string> Lines = new List<string> { "" };

        int _cursorX;
        int _cursorY;

        int _scrollingPixelsX;
        int _scrollingPixelsY;

        public TextEditor(Desktop desktop, Element parent)
            : base(desktop, parent)
        {
        }

        public override Element HitTest(int x, int y) => LayoutRectangle.Contains(x, y) ? this : null;

        public override void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_LEFT: GoLeft(); break;
                case SDL.SDL_Keycode.SDLK_RIGHT: GoRight(); break;
                case SDL.SDL_Keycode.SDLK_UP: GoUp(); break;
                case SDL.SDL_Keycode.SDLK_DOWN: GoDown(); break;
                case SDL.SDL_Keycode.SDLK_BACKSPACE: Erase(); break;
                case SDL.SDL_Keycode.SDLK_DELETE: Delete(); break;
                case SDL.SDL_Keycode.SDLK_RETURN: BreakLine(); break;
                case SDL.SDL_Keycode.SDLK_TAB: InsertText("  "); break;
                default: base.OnKeyDown(key, repeat); break;
            }

            void GoLeft()
            {
                if (_cursorX > 0) _cursorX--;
                else if (_cursorY > 0)
                {
                    _cursorY--;
                    _cursorX = Lines[_cursorY].Length;
                }

                ClampScrolling();
            }

            void GoRight()
            {
                if (_cursorX < Lines[_cursorY].Length) _cursorX++;
                else if (_cursorY < Lines.Count - 1)
                {
                    _cursorY++;
                    _cursorX = 0;
                }

                ClampScrolling();
            }

            void GoUp()
            {
                if (_cursorY > 0)
                {
                    _cursorY--;
                    _cursorX = Math.Min(_cursorX, Lines[_cursorY].Length);
                }
                else
                {
                    _cursorX = 0;
                }

                ClampScrolling();
            }

            void GoDown()
            {
                if (_cursorY < Lines.Count - 1)
                {
                    _cursorY++;
                    _cursorX = Math.Min(_cursorX, Lines[_cursorY].Length);
                }
                else
                {
                    _cursorX = Lines[_cursorY].Length;
                }

                ClampScrolling();
            }

            void Erase()
            {
                var line = Lines[_cursorY];

                if (_cursorX > 0)
                {
                    Lines[_cursorY] = line[0..(_cursorX - 1)] + line[_cursorX..];
                    _cursorX--;
                }
                else if (_cursorY > 0)
                {
                    Lines.RemoveAt(_cursorY);
                    _cursorY--;
                    _cursorX = Lines[_cursorY].Length;
                    Lines[_cursorY] += line;
                }

                ClampScrolling();
            }

            void Delete()
            {
                var line = Lines[_cursorY];

                if (_cursorX < line.Length)
                {
                    Lines[_cursorY] = line[0.._cursorX] + line[(_cursorX + 1)..];
                }
                else if (_cursorY < Lines.Count - 1)
                {
                    Lines[_cursorY] += Lines[_cursorY + 1];
                    Lines.RemoveAt(_cursorY + 1);
                }

                ClampScrolling();
            }

            void BreakLine()
            {
                if (_cursorX == 0)
                {
                    Lines.Insert(_cursorY, "");
                    _cursorX = 0;
                    _cursorY++;
                }
                else if (_cursorX == Lines[_cursorY].Length)
                {
                    _cursorX = 0;
                    _cursorY++;
                    Lines.Insert(_cursorY, "");
                }
                else
                {
                    var endOfLine = Lines[_cursorY][_cursorX..];
                    Lines[_cursorY] = Lines[_cursorY][0.._cursorX];
                    _cursorX = 0;
                    _cursorY++;
                    Lines.Insert(_cursorY, endOfLine);
                }

                ClampScrolling();
            }
        }

        public override void OnTextEntered(string text)
        {
            InsertText(text);
        }

        public override void OnMouseDown(int button)
        {
            if (button == 1)
            {
                Desktop.FocusedElement = this;
                var x = Desktop.MouseX + _scrollingPixelsX - LayoutRectangle.X;
                var y = Desktop.MouseY + _scrollingPixelsY - LayoutRectangle.Y;
            }
        }

        public override void OnMouseWheel(int dx, int dy)
        {
            if (dy != 0)
            {
                _scrollingPixelsY -= dy * 16;
                ClampScrolling();
            }
        }

        void InsertText(string text)
        {
            var line = Lines[_cursorY];
            Lines[_cursorY] = line[0.._cursorX] + text + line[_cursorX..];
            _cursorX += text.Length;
            ClampScrolling();
        }

        void ClampScrolling()
        {
            _scrollingPixelsY = Math.Clamp(_scrollingPixelsY,
                Math.Max(0, (_cursorY + 1) * RendererHelper.FontRenderSize - LayoutRectangle.Height),
                Math.Max(0, Math.Min(_cursorY * RendererHelper.FontRenderSize, Lines.Count * RendererHelper.FontRenderSize - LayoutRectangle.Height)));
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            var startY = _scrollingPixelsY / RendererHelper.FontRenderSize;
            var endY = Math.Min(Lines.Count - 1, (_scrollingPixelsY + LayoutRectangle.Height) / RendererHelper.FontRenderSize);

            var clipRect = Desktop.ToSDL_Rect(LayoutRectangle);
            SDL.SDL_RenderSetClipRect(Desktop.Renderer, ref clipRect);

            for (var y = startY; y <= endY; y++)
            {
                RendererHelper.DrawText(Desktop.Renderer,
                    LayoutRectangle.X - _scrollingPixelsX,
                    LayoutRectangle.Y + y * RendererHelper.FontRenderSize - _scrollingPixelsY,
                    Lines[y], TextColor);
            }

            SDL.SDL_RenderSetClipRect(Desktop.Renderer, IntPtr.Zero);

            if (Desktop.FocusedElement == this)
            {
                new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);

                var cursorRect = Desktop.ToSDL_Rect(new DeepSwarmCommon.Rectangle(
                    LayoutRectangle.X + _cursorX * RendererHelper.FontRenderSize - _scrollingPixelsX,
                    LayoutRectangle.Y + _cursorY * RendererHelper.FontRenderSize - _scrollingPixelsY,
                    2, RendererHelper.FontRenderSize));
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref cursorRect);
            }
        }

        public void SetText(string text)
        {
            Lines.Clear();
            Lines.AddRange(text.Replace("\r", "").Split("\n"));

            _cursorX = 0;
            _cursorY = 0;
        }
    }
}
