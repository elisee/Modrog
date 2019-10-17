﻿using DeepSwarmBasics;
using DeepSwarmBasics.Math;
using DeepSwarmPlatform.Graphics;
using SDL2;
using System;
using System.Collections.Generic;

namespace DeepSwarmPlatform.UI
{
    public class TextEditor : Element
    {
        public FontStyle FontStyle;
        public Color TextColor = new Color(0xffffffff);
        public Point CellSize = new Point(12, 20);

        public readonly List<string> Lines = new List<string> { "" };

        Point _cursor;
        Point _selectionAnchor;
        float _cursorTimer;

        Point _scrollingPixels;

        public TextEditor(Element parent) : this(parent.Desktop, parent) { }
        public TextEditor(Desktop desktop, Element parent) : base(desktop, parent)
        {
            FontStyle = Desktop.MonoFontStyle;
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
        void Animate(float deltaTime)
        {
            _cursorTimer += deltaTime;
        }

        void InsertText(string text)
        {
            EraseSelection();
            var line = Lines[_cursor.Y];
            Lines[_cursor.Y] = line[0.._cursor.X] + text + line[_cursor.X..];
            _cursor.X += text.Length;

            ClearSelection();
            ClampScrolling();
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

        void ClearSelection() { _selectionAnchor = _cursor; _cursorTimer = 0f; }
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
            var x = Desktop.MouseX + _scrollingPixels.X - RectangleAfterPadding.X;
            var y = Desktop.MouseY + _scrollingPixels.Y - RectangleAfterPadding.Y;

            var targetX = x / CellSize.X;
            var targetY = y / CellSize.Y;

            var wasRightHalfClicked = x % CellSize.X > CellSize.X / 2;
            if (wasRightHalfClicked) targetX++;

            targetY = Math.Clamp(targetY, 0, Lines.Count - 1);
            targetX = Math.Clamp(targetX, 0, Lines[targetY].Length);

            return new Point(targetX, targetY);
        }

        void ClampScrolling()
        {
            _scrollingPixels.Y = Math.Clamp(_scrollingPixels.Y,
                Math.Max(0, (_cursor.Y + 1) * CellSize.Y - RectangleAfterPadding.Height),
                Math.Max(0, Math.Min(_cursor.Y * CellSize.Y, Lines.Count * CellSize.Y - RectangleAfterPadding.Height)));
        }
        #endregion

        #region Events
        public override Element HitTest(int x, int y) => LayoutRectangle.Contains(x, y) ? this : null;

        public override void OnMouseEnter() => SDL.SDL_SetCursor(Cursors.IBeamCursor);
        public override void OnMouseExit() => SDL.SDL_SetCursor(Cursors.ArrowCursor);

        public override void OnFocus() { _cursorTimer = 0f; Desktop.RegisterAnimation(Animate); }
        public override void OnBlur() { Desktop.UnregisterAnimation(Animate); }

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

            if (Desktop.IsCtrlDown)
            {
                switch (key)
                {
                    case SDL.SDL_Keycode.SDLK_a: SelectAll(); break;
                }
            }

            void GoLeft()
            {
                if (_cursor.X > 0) _cursor.X--;
                else if (_cursor.Y > 0)
                {
                    _cursor.Y--;
                    _cursor.X = Lines[_cursor.Y].Length;
                }

                ClampScrolling();
                ClearSelectionUnlessShiftDown();
            }

            void GoRight()
            {
                if (_cursor.X < Lines[_cursor.Y].Length) _cursor.X++;
                else if (_cursor.Y < Lines.Count - 1)
                {
                    _cursor.Y++;
                    _cursor.X = 0;
                }

                ClampScrolling();
                ClearSelectionUnlessShiftDown();
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

                ClampScrolling();
                ClearSelectionUnlessShiftDown();
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

                ClampScrolling();
                ClearSelectionUnlessShiftDown();
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

                ClampScrolling();
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

                ClampScrolling();
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

                ClampScrolling();
                ClearSelection();
            }

            void GoToStartOfLine()
            {
                _cursor.X = 0;
                ClearSelectionUnlessShiftDown();
            }

            void GoToEndOfLine()
            {
                _cursor.X = Lines[_cursor.Y].Length;
                ClearSelectionUnlessShiftDown();
            }

            void ClearSelectionUnlessShiftDown()
            {
                if (!Desktop.IsShiftDown)
                    ClearSelection();

                _cursorTimer = 0f;
            }

            void SelectAll()
            {
                _selectionAnchor = Point.Zero;
                _cursor.Y = Lines.Count - 1;
                _cursor.X = Lines[_cursor.Y].Length;
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
                Desktop.SetFocusedElement(this);
                Desktop.SetHoveredElementPressed(true);

                _cursor = _selectionAnchor = GetHoveredTextPosition();
                ClampScrolling();
            }
        }

        public override void OnMouseMove()
        {
            if (IsPressed)
            {
                _cursor = GetHoveredTextPosition();

                var mouseY = Desktop.MouseY;
                if (mouseY < RectangleAfterPadding.Top) Scroll(1);
                if (mouseY > RectangleAfterPadding.Bottom) Scroll(-1);

                ClampScrolling();
            }
        }

        public override void OnMouseUp(int button)
        {
            if (button == 1 && IsPressed)
            {
                Desktop.SetHoveredElementPressed(false);
                ClampScrolling();
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
                _scrollingPixels.Y -= dy * CellSize.Y * 3;
                ClampScrolling();
            }
        }
        #endregion

        #region Drawing
        protected override void DrawSelf()
        {
            base.DrawSelf();

            var startY = _scrollingPixels.Y / CellSize.Y;
            var endY = Math.Min(Lines.Count - 1, (_scrollingPixels.Y + RectangleAfterPadding.Height) / CellSize.Y);

            var clipRect = RectangleAfterPadding.ToSDL_Rect();
            SDL.SDL_RenderSetClipRect(Desktop.Renderer, ref clipRect);

            // Draw selection
            if (HasSelection())
            {
                new Color(0x0000ffaa).UseAsDrawColor(Desktop.Renderer);
                GetSelectionRange(out var firstPosition, out var lastPosition);

                if (firstPosition.Y == lastPosition.Y)
                {
                    var selectionRect = new Rectangle(
                    RectangleAfterPadding.X + firstPosition.X * CellSize.X - _scrollingPixels.X,
                    RectangleAfterPadding.Y + firstPosition.Y * CellSize.Y - _scrollingPixels.Y,
                    (lastPosition.X - firstPosition.X) * CellSize.X, CellSize.Y).ToSDL_Rect();
                    SDL.SDL_RenderFillRect(Desktop.Renderer, ref selectionRect);
                }
                else
                {
                    var firstLineSelectionRect = new Rectangle(
                    RectangleAfterPadding.X + firstPosition.X * CellSize.X - _scrollingPixels.X,
                    RectangleAfterPadding.Y + firstPosition.Y * CellSize.Y - _scrollingPixels.Y,
                    (Lines[firstPosition.Y][firstPosition.X..].Length) * CellSize.X, CellSize.Y).ToSDL_Rect();

                    for (int i = firstPosition.Y + 1; i < lastPosition.Y; i++)
                    {
                        var midSelectionRect = new Rectangle(
                            RectangleAfterPadding.X + 0 - _scrollingPixels.X,
                            RectangleAfterPadding.Y + i * CellSize.Y - _scrollingPixels.Y,
                            (Lines[i].Length) * CellSize.X, CellSize.Y).ToSDL_Rect();
                        SDL.SDL_RenderFillRect(Desktop.Renderer, ref midSelectionRect);
                    }

                    var lastLineSelectionRect = new Rectangle(
                    RectangleAfterPadding.X + 0 - _scrollingPixels.X,
                    RectangleAfterPadding.Y + lastPosition.Y * CellSize.Y - _scrollingPixels.Y,
                    (Lines[lastPosition.Y][..lastPosition.X].Length) * CellSize.X, CellSize.Y).ToSDL_Rect();

                    SDL.SDL_RenderFillRect(Desktop.Renderer, ref firstLineSelectionRect);
                    SDL.SDL_RenderFillRect(Desktop.Renderer, ref lastLineSelectionRect);
                }
            }

            // Draw text
            TextColor.UseAsDrawColor(Desktop.Renderer);
            for (var y = startY; y <= endY; y++)
            {
                FontStyle.DrawText(
                    RectangleAfterPadding.X - _scrollingPixels.X,
                    RectangleAfterPadding.Y + y * CellSize.Y + FontStyle.LineSpacing / 2 - _scrollingPixels.Y,
                    Lines[y]);
            }

            SDL.SDL_RenderSetClipRect(Desktop.Renderer, IntPtr.Zero);

            // Draw cursor
            if (Desktop.FocusedElement == this && (_cursorTimer % TextInput.CursorFlashInterval * 2) < TextInput.CursorFlashInterval)
            {
                new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);

                var cursorRect = new Rectangle(
                    RectangleAfterPadding.X + _cursor.X * CellSize.X - _scrollingPixels.X,
                    RectangleAfterPadding.Y + _cursor.Y * CellSize.Y - _scrollingPixels.Y,
                    2, CellSize.Y).ToSDL_Rect();
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref cursorRect);
            }
        }
        #endregion
    }
}
