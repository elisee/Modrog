using DeepSwarmCommon;
using SDL2;
using System;
using System.Collections.Generic;

namespace DeepSwarmClient.UI
{
    class TextEditor : Element
    {
        public Color TextColor = new Color(0xffffffff);

        public readonly List<string> Lines = new List<string> { "" };

        Point _cursor;
        Point _selectionAnchor;

        Point _scrollingPixels;

        public TextEditor(Desktop desktop, Element parent)
            : base(desktop, parent)
        {
        }

        #region Configuration
        public void SetText(string text)
        {
            Lines.Clear();
            Lines.AddRange(text.Replace("\r", "").Split("\n"));
            _scrollingPixels = Point.Zero;

            _cursor = _selectionAnchor = Point.Zero;
        }

        public string GetText() => string.Join('\n', Lines);
        #endregion

        #region Internals
        void InsertText(string text)
        {
            EraseSelection();
            var line = Lines[_cursor.Y];
            Lines[_cursor.Y] = line[0.._cursor.X] + text + line[_cursor.X..];
            _cursor.X += text.Length;

            ClearSelection();
            ClampScrollToCursor();
        }

        void GetSelectionRange(out Point start, out Point end)
        {
            start = _selectionAnchor;
            end = _cursor;

            if (start.Y > end.Y || (start.Y == end.Y && start.X > end.X))
            {
                start = _cursor;
                end = _selectionAnchor;
            }
        }

        void ClearSelection() { _selectionAnchor = _cursor; }
        bool HasSelection() => _cursor != _selectionAnchor;

        void EraseSelection()
        {
            GetSelectionRange(out var selectionStart, out var selectionEnd);

            if (selectionStart.Y == selectionEnd.Y)
            {
                var line = Lines[selectionStart.Y];
                Lines[selectionStart.Y] = line[0..selectionStart.X] + line[selectionEnd.X..];
            }
            else
            {
                var firstLine = Lines[selectionStart.Y];
                var lastLine = Lines[selectionEnd.Y];

                for (int i = selectionStart.Y + 1; i <= selectionEnd.Y; i++)
                {
                    Lines.RemoveAt(selectionStart.Y + 1);
                }

                Lines[selectionStart.Y] = firstLine[0..selectionStart.X] + lastLine[selectionEnd.X..];
            }

            _cursor = _selectionAnchor = selectionStart;
        }

        Point GetHoveredTextPosition()
        {
            var x = Desktop.MouseX + _scrollingPixels.X - LayoutRectangle.X;
            var y = Desktop.MouseY + _scrollingPixels.Y - LayoutRectangle.Y;

            var targetX = x / RendererHelper.FontRenderSize;
            var targetY = y / RendererHelper.FontRenderSize;

            var wasRightHalfClicked = x % RendererHelper.FontRenderSize > RendererHelper.FontRenderSize / 2;
            if (wasRightHalfClicked) targetX++;

            targetY = Math.Clamp(targetY, 0, Lines.Count - 1);
            targetX = Math.Clamp(targetX, 0, Lines[targetY].Length);

            return new Point(targetX, targetY);
        }

        void ClampScrollToCursor()
        {
            _scrollingPixels.Y = Math.Clamp(_scrollingPixels.Y,
                Math.Max(0, (_cursor.Y + 1) * RendererHelper.FontRenderSize - LayoutRectangle.Height),
                Math.Max(0, Math.Min(_cursor.Y * RendererHelper.FontRenderSize, Lines.Count * RendererHelper.FontRenderSize - LayoutRectangle.Height)));
        }
        #endregion

        #region Events
        public override Element HitTest(int x, int y) => LayoutRectangle.Contains(x, y) ? this : null;

        public override void OnMouseEnter()
        {
            SDL.SDL_SetCursor(RendererHelper.IbeamCursor);
        }
        public override void OnMouseExit()
        {
            SDL.SDL_SetCursor(RendererHelper.ArrowCursor);
        }

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
                case SDL.SDL_Keycode.SDLK_HOME: GoToStartOfLine(); break;
                case SDL.SDL_Keycode.SDLK_END: GoToEndOfLine(); break;
                default: base.OnKeyDown(key, repeat); break;
            }

            void GoLeft()
            {
                if (_cursor.X > 0) _cursor.X--;
                else if (_cursor.Y > 0)
                {
                    _cursor.Y--;
                    _cursor.X = Lines[_cursor.Y].Length;
                }

                ClampScrollToCursor();
                ClearSelection();
            }

            void GoRight()
            {
                if (_cursor.X < Lines[_cursor.Y].Length) _cursor.X++;
                else if (_cursor.Y < Lines.Count - 1)
                {
                    _cursor.Y++;
                    _cursor.X = 0;
                }

                ClampScrollToCursor();
                ClearSelection();
            }

            void GoUp()
            {
                if (_cursor.Y > 0)
                {
                    _cursor.Y--;
                    _cursor.X = Math.Min(_cursor.X, Lines[_cursor.Y].Length);
                }
                else
                {
                    _cursor.X = 0;
                }

                ClampScrollToCursor();
                ClearSelection();
            }

            void GoDown()
            {
                if (_cursor.Y < Lines.Count - 1)
                {
                    _cursor.Y++;
                    _cursor.X = Math.Min(_cursor.X, Lines[_cursor.Y].Length);
                }
                else
                {
                    _cursor.X = Lines[_cursor.Y].Length;
                }

                ClampScrollToCursor();
                ClearSelection();
            }

            void Erase()
            {
                if (!HasSelection())
                {
                    var line = Lines[_cursor.Y];

                    if (_cursor.X > 0)
                    {
                        Lines[_cursor.Y] = line[0..(_cursor.X - 1)] + line[_cursor.X..];
                        _cursor.X--;
                    }
                    else if (_cursor.Y > 0)
                    {
                        Lines.RemoveAt(_cursor.Y);
                        _cursor.Y--;
                        _cursor.X = Lines[_cursor.Y].Length;
                        Lines[_cursor.Y] += line;
                    }
                }
                else
                {
                    EraseSelection();
                }

                ClampScrollToCursor();
                ClearSelection();
            }

            void Delete()
            {
                if (!HasSelection())
                {
                    var line = Lines[_cursor.Y];

                    if (_cursor.X < line.Length)
                    {
                        Lines[_cursor.Y] = line[0.._cursor.X] + line[(_cursor.X + 1)..];
                    }
                    else if (_cursor.Y < Lines.Count - 1)
                    {
                        Lines[_cursor.Y] += Lines[_cursor.Y + 1];
                        Lines.RemoveAt(_cursor.Y + 1);
                    }
                }
                else
                {
                    EraseSelection();
                }

                ClampScrollToCursor();
                ClearSelection();
            }

            void BreakLine()
            {
                EraseSelection();
                if (_cursor.X == 0)
                {
                    Lines.Insert(_cursor.Y, "");
                    _cursor.X = 0;
                    _cursor.Y++;
                }
                else if (_cursor.X == Lines[_cursor.Y].Length)
                {
                    _cursor.X = 0;
                    _cursor.Y++;
                    Lines.Insert(_cursor.Y, "");
                }
                else
                {
                    var endOfLine = Lines[_cursor.Y][_cursor.X..];
                    Lines[_cursor.Y] = Lines[_cursor.Y][0.._cursor.X];
                    _cursor.X = 0;
                    _cursor.Y++;
                    Lines.Insert(_cursor.Y, endOfLine);
                }

                ClampScrollToCursor();
                ClearSelection();
            }

            void GoToStartOfLine()
            {
                _cursor.X = 0;
                ClearSelection();
            }

            void GoToEndOfLine()
            {
                _cursor.X = Lines[_cursor.Y].Length;
                ClearSelection();
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
                Desktop.SetFocusedElement(this);
                Desktop.SetHoveredElementPressed(true);

                _cursor = _selectionAnchor = GetHoveredTextPosition();
            }
        }

        public override void OnMouseMove()
        {
            if (IsPressed)
            {
                _cursor = GetHoveredTextPosition();

                var mouseY = Desktop.MouseY;
                if (mouseY < LayoutRectangle.Top) Scroll(1);
                if (mouseY > LayoutRectangle.Bottom) Scroll(-1);

                ClampScrollToCursor();
            }
        }

        public override void OnMouseUp(int button)
        {
            if (button == 1 && IsPressed)
            {
                Desktop.SetHoveredElementPressed(false);
            }
        }

        public override void OnMouseWheel(int dx, int dy)
        {
            Scroll(dy);
        }

        void Scroll(int dy)
        {
            if (dy != 0)
            {
                _scrollingPixels.Y -= dy * 16;
                ClampScrollToCursor();
            }
        }
        #endregion

        #region Drawing
        protected override void DrawSelf()
        {
            base.DrawSelf();

            var startY = _scrollingPixels.Y / RendererHelper.FontRenderSize;
            var endY = Math.Min(Lines.Count - 1, (_scrollingPixels.Y + LayoutRectangle.Height) / RendererHelper.FontRenderSize);

            var clipRect = Desktop.ToSDL_Rect(LayoutRectangle);
            SDL.SDL_RenderSetClipRect(Desktop.Renderer, ref clipRect);

            // Draw selection
            if (HasSelection())
            {
                new Color(0x0000ffaa).UseAsDrawColor(Desktop.Renderer);
                GetSelectionRange(out var firstPosition, out var lastPosition);

                if (firstPosition.Y == lastPosition.Y)
                {
                    var selectionRect = Desktop.ToSDL_Rect(new Rectangle(
                    LayoutRectangle.X + firstPosition.X * RendererHelper.FontRenderSize - _scrollingPixels.X,
                    LayoutRectangle.Y + firstPosition.Y * RendererHelper.FontRenderSize - _scrollingPixels.Y,
                    (lastPosition.X - firstPosition.X) * RendererHelper.FontRenderSize, RendererHelper.FontRenderSize));
                    SDL.SDL_RenderFillRect(Desktop.Renderer, ref selectionRect);
                }
                else
                {
                    var firstLineSelectionRect = Desktop.ToSDL_Rect(new Rectangle(
                    LayoutRectangle.X + firstPosition.X * RendererHelper.FontRenderSize - _scrollingPixels.X,
                    LayoutRectangle.Y + firstPosition.Y * RendererHelper.FontRenderSize - _scrollingPixels.Y,
                    (Lines[firstPosition.Y][firstPosition.X..].Length) * RendererHelper.FontRenderSize, RendererHelper.FontRenderSize));

                    for (int i = firstPosition.Y + 1; i < lastPosition.Y; i++)
                    {
                        var midSelectionRect = Desktop.ToSDL_Rect(new Rectangle(
                            LayoutRectangle.X + 0 - _scrollingPixels.X,
                            LayoutRectangle.Y + i * RendererHelper.FontRenderSize - _scrollingPixels.Y,
                            (Lines[i].Length) * RendererHelper.FontRenderSize, RendererHelper.FontRenderSize));
                        SDL.SDL_RenderFillRect(Desktop.Renderer, ref midSelectionRect);
                    }

                    var lastLineSelectionRect = Desktop.ToSDL_Rect(new Rectangle(
                    LayoutRectangle.X + 0 - _scrollingPixels.X,
                    LayoutRectangle.Y + lastPosition.Y * RendererHelper.FontRenderSize - _scrollingPixels.Y,
                    (Lines[lastPosition.Y][..lastPosition.X].Length) * RendererHelper.FontRenderSize, RendererHelper.FontRenderSize));

                    SDL.SDL_RenderFillRect(Desktop.Renderer, ref firstLineSelectionRect);
                    SDL.SDL_RenderFillRect(Desktop.Renderer, ref lastLineSelectionRect);
                }
            }

            // Draw text
            for (var y = startY; y <= endY; y++)
            {
                RendererHelper.DrawText(Desktop.Renderer,
                    LayoutRectangle.X - _scrollingPixels.X,
                    LayoutRectangle.Y + y * RendererHelper.FontRenderSize - _scrollingPixels.Y,
                    Lines[y], TextColor);
            }

            SDL.SDL_RenderSetClipRect(Desktop.Renderer, IntPtr.Zero);

            // Draw cursor
            if (Desktop.FocusedElement == this)
            {
                new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);

                var cursorRect = Desktop.ToSDL_Rect(new Rectangle(
                    LayoutRectangle.X + _cursor.X * RendererHelper.FontRenderSize - _scrollingPixels.X,
                    LayoutRectangle.Y + _cursor.Y * RendererHelper.FontRenderSize - _scrollingPixels.Y,
                    2, RendererHelper.FontRenderSize));
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref cursorRect);
            }
        }
        #endregion
    }
}
