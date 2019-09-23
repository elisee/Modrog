using SDL2;
using System;
using System.Collections.Generic;

namespace DeepSwarmClient.UI
{
    class TextEditor : Element
    {
        public Color TextColor = new Color(0xffffffff);

        public readonly List<string> Lines = new List<string> { "" };

        public int CursorX;
        public int CursorY;

        public int ScrollingPixelsX;
        public int ScrollingPixelsY;

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
                if (CursorX > 0) CursorX--;
                else if (CursorY > 0)
                {
                    CursorY--;
                    CursorX = Lines[CursorY].Length;
                }

                ClampScrolling();
            }

            void GoRight()
            {
                if (CursorX < Lines[CursorY].Length) CursorX++;
                else if (CursorY < Lines.Count - 1)
                {
                    CursorY++;
                    CursorX = 0;
                }

                ClampScrolling();
            }

            void GoUp()
            {
                if (CursorY > 0)
                {
                    CursorY--;
                    CursorX = Math.Min(CursorX, Lines[CursorY].Length);
                }
                else
                {
                    CursorX = 0;
                }

                ClampScrolling();
            }

            void GoDown()
            {
                if (CursorY < Lines.Count - 1)
                {
                    CursorY++;
                    CursorX = Math.Min(CursorX, Lines[CursorY].Length);
                }
                else
                {
                    CursorX = Lines[CursorY].Length;
                }

                ClampScrolling();
            }

            void Erase()
            {
                var line = Lines[CursorY];

                if (CursorX > 0)
                {
                    Lines[CursorY] = line.Substring(0, CursorX - 1) + line.Substring(CursorX);
                    CursorX--;
                }
                else if (CursorY > 0)
                {
                    Lines.RemoveAt(CursorY);
                    CursorY--;
                    Lines[CursorY] += line;
                    CursorX = Lines[CursorY].Length;
                }

                ClampScrolling();
            }

            void Delete()
            {
                var line = Lines[CursorY];

                if (CursorX < line.Length)
                {
                    Lines[CursorY] = line.Substring(0, CursorX) + line.Substring(CursorX + 1);
                }
                else if (CursorY < Lines.Count - 1)
                {
                    Lines[CursorY] += Lines[CursorY + 1];
                    Lines.RemoveAt(CursorY + 1);
                }

                ClampScrolling();
            }

            void BreakLine()
            {
                if (CursorX == 0)
                {
                    Lines.Insert(CursorY, "");
                    CursorX = 0;
                    CursorY++;
                }
                else if (CursorX == Lines[CursorY].Length)
                {
                    CursorX = 0;
                    CursorY++;
                    Lines.Insert(CursorY, "");
                }
                else
                {
                    var endOfLine = Lines[CursorY].Substring(CursorX);
                    Lines[CursorY] = Lines[CursorY].Substring(0, CursorX);
                    CursorX = 0;
                    CursorY++;
                    Lines.Insert(CursorY, endOfLine);
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
                var x = Desktop.MouseX + ScrollingPixelsX - LayoutRectangle.X;
                var y = Desktop.MouseY + ScrollingPixelsY - LayoutRectangle.Y;
            }
        }

        public override void OnMouseWheel(int dx, int dy)
        {
            if (dy != 0)
            {
                ScrollingPixelsY -= dy * 16;
                ClampScrolling();
            }
        }

        void InsertText(string text)
        {
            var line = Lines[CursorY];
            Lines[CursorY] = line.Substring(0, CursorX) + text + line.Substring(CursorX);
            CursorX += text.Length;
            ClampScrolling();
        }

        void ClampScrolling()
        {
            ScrollingPixelsY = Math.Clamp(ScrollingPixelsY,
                Math.Max(0, (CursorY + 1) * RendererHelper.FontRenderSize - LayoutRectangle.Height),
                Math.Max(0, Math.Min(CursorY * RendererHelper.FontRenderSize, Lines.Count * RendererHelper.FontRenderSize - LayoutRectangle.Height)));
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            var startY = ScrollingPixelsY / RendererHelper.FontRenderSize;
            var endY = Math.Min(Lines.Count - 1, (ScrollingPixelsY + LayoutRectangle.Height) / RendererHelper.FontRenderSize);

            var clipRect = Desktop.ToSDL_Rect(LayoutRectangle);
            SDL.SDL_RenderSetClipRect(Desktop.Renderer, ref clipRect);

            for (var y = startY; y <= endY; y++)
            {
                RendererHelper.DrawText(Desktop.Renderer,
                    LayoutRectangle.X - ScrollingPixelsX,
                    LayoutRectangle.Y + y * RendererHelper.FontRenderSize - ScrollingPixelsY,
                    Lines[y], TextColor);
            }

            SDL.SDL_RenderSetClipRect(Desktop.Renderer, IntPtr.Zero);

            if (Desktop.FocusedElement == this)
            {
                new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);

                var cursorRect = Desktop.ToSDL_Rect(new DeepSwarmCommon.Rectangle(
                    LayoutRectangle.X + CursorX * RendererHelper.FontRenderSize - ScrollingPixelsX,
                    LayoutRectangle.Y + CursorY * RendererHelper.FontRenderSize - ScrollingPixelsY,
                    2, RendererHelper.FontRenderSize));
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref cursorRect);
            }
        }

        public void SetText(string text)
        {
            Lines.Clear();
            Lines.AddRange(text.Replace("\r", "").Split("\n"));

            CursorX = 0;
            CursorY = 0;
        }
    }
}
