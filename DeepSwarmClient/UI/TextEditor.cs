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
            }

            void GoRight()
            {
                if (CursorX < Lines[CursorY].Length) CursorX++;
                else if (CursorY < Lines.Count - 1)
                {
                    CursorY++;
                    CursorX = 0;
                }
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
            }
        }

        public override void OnTextEntered(string text)
        {
            InsertText(text);
        }

        void InsertText(string text)
        {
            var line = Lines[CursorY];
            Lines[CursorY] = line.Substring(0, CursorX) + text + line.Substring(CursorX);
            CursorX += text.Length;
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            for (var y = 0; y < Lines.Count; y++)
            {
                RendererHelper.DrawText(Desktop.Renderer, LayoutRectangle.X, LayoutRectangle.Y + y * RendererHelper.FontRenderSize, Lines[y], TextColor);
            }

            if (Desktop.FocusedElement == this)
            {
                new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);

                var cursorRect = Desktop.ToSDL_Rect(new DeepSwarmCommon.Rectangle(
                    LayoutRectangle.X + CursorX * RendererHelper.FontRenderSize, LayoutRectangle.Y + CursorY * RendererHelper.FontRenderSize,
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
