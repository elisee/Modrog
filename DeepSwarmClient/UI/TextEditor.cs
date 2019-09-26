using SDL2;
using System;
using System.Collections.Generic;
using DeepSwarmCommon;

namespace DeepSwarmClient.UI
{
    class TextEditor : Element
    {
        public Color TextColor = new Color(0xffffffff);

        public readonly List<string> Lines = new List<string> { "" };

        Point _cursorPosition = new Point();
        Point _selectionStartPosition = new Point();

        int _scrollingPixelsX;
        int _scrollingPixelsY;

        private bool _isMakingSelection;

        public TextEditor(Desktop desktop, Element parent)
            : base(desktop, parent)
        {
        }

        public override Element HitTest(int x, int y) => LayoutRectangle.Contains(x, y) ? this : null;

        public override void OnMouseEnter()
        {
            SDL2.SDL.SDL_SetCursor(RendererHelper.IbeamCursor);
        }
        public override void OnMouseExit()
        {
            SDL2.SDL.SDL_SetCursor(RendererHelper.ArrowCursor);
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
                if (_cursorPosition.X > 0) _cursorPosition.X--;
                else if (_cursorPosition.Y > 0)
                {
                    _cursorPosition.Y--;
                    _cursorPosition.X = Lines[_cursorPosition.Y].Length;
                }

                ClampScrolling();
                CancelSelection();
            }

            void GoRight()
            {
                if (_cursorPosition.X < Lines[_cursorPosition.Y].Length) _cursorPosition.X++;
                else if (_cursorPosition.Y < Lines.Count - 1)
                {
                    _cursorPosition.Y++;
                    _cursorPosition.X = 0;
                }

                ClampScrolling();
                CancelSelection();
            }

            void GoUp()
            {
                if (_cursorPosition.Y > 0)
                {
                    _cursorPosition.Y--;
                    _cursorPosition.X = Math.Min(_cursorPosition.X, Lines[_cursorPosition.Y].Length);
                }
                else
                {
                    _cursorPosition.X = 0;
                }

                ClampScrolling();
                CancelSelection();
            }

            void GoDown()
            {
                if (_cursorPosition.Y < Lines.Count - 1)
                {
                    _cursorPosition.Y++;
                    _cursorPosition.X = Math.Min(_cursorPosition.X, Lines[_cursorPosition.Y].Length);
                }
                else
                {
                    _cursorPosition.X = Lines[_cursorPosition.Y].Length;
                }

                ClampScrolling();
                CancelSelection();
            }

            void Erase()
            {
                if (!HasSelection())
                {
                    var line = Lines[_cursorPosition.Y];

                    if (_cursorPosition.X > 0)
                    {
                        Lines[_cursorPosition.Y] = line[0..(_cursorPosition.X - 1)] + line[_cursorPosition.X..];
                        _cursorPosition.X--;
                    }
                    else if (_cursorPosition.Y > 0)
                    {
                        Lines.RemoveAt(_cursorPosition.Y);
                        _cursorPosition.Y--;
                        _cursorPosition.X = Lines[_cursorPosition.Y].Length;
                        Lines[_cursorPosition.Y] += line;
                    }
                }
                else
                {
                    EraseSelection();
                }

                ClampScrolling();
                CancelSelection();
            }

            void Delete()
            {
                if (!HasSelection())
                {
                    var line = Lines[_cursorPosition.Y];

                    if (_cursorPosition.X < line.Length)
                    {
                        Lines[_cursorPosition.Y] = line[0.._cursorPosition.X] + line[(_cursorPosition.X + 1)..];
                    }
                    else if (_cursorPosition.Y < Lines.Count - 1)
                    {
                        Lines[_cursorPosition.Y] += Lines[_cursorPosition.Y + 1];
                        Lines.RemoveAt(_cursorPosition.Y + 1);
                    }
                }
                else
                {
                    EraseSelection();
                }

                ClampScrolling();
                CancelSelection();
            }

            void BreakLine()
            {
                EraseSelection();
                if (_cursorPosition.X == 0)
                {
                    Lines.Insert(_cursorPosition.Y, "");
                    _cursorPosition.X = 0;
                    _cursorPosition.Y++;
                }
                else if (_cursorPosition.X == Lines[_cursorPosition.Y].Length)
                {
                    _cursorPosition.X = 0;
                    _cursorPosition.Y++;
                    Lines.Insert(_cursorPosition.Y, "");
                }
                else
                {
                    var endOfLine = Lines[_cursorPosition.Y][_cursorPosition.X..];
                    Lines[_cursorPosition.Y] = Lines[_cursorPosition.Y][0.._cursorPosition.X];
                    _cursorPosition.X = 0;
                    _cursorPosition.Y++;
                    Lines.Insert(_cursorPosition.Y, endOfLine);
                }

                ClampScrolling();
                CancelSelection();
            }

            void GoToStartOfLine()
            {
                _cursorPosition.X = 0;
                CancelSelection();
            }

            void GoToEndOfLine()
            {
                _cursorPosition.X = Lines[_cursorPosition.Y].Length;
                CancelSelection();
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
                _isMakingSelection = true;
                _cursorPosition = GetCursorPositionFromMousePosition();

                CancelSelection();
            }
        }

        public override void OnMouseMove()
        {
            if (_isMakingSelection)
            {
                _cursorPosition = GetCursorPositionFromMousePosition();
            }
        }

        public override void OnMouseUp(int button)
        {
            if (button == 1)
            {
                Desktop.SetFocusedElement(this);
                _isMakingSelection = false;
            }
        }

        public void EraseSelection()
        {
            var firstPosition = new Point();
            var lastPosition = new Point();
            bool bothCursorsOnSameLine = GetTextCursorPositionsInOrder(ref firstPosition, ref lastPosition);

            if (bothCursorsOnSameLine)
            {
                string line = Lines[firstPosition.Y];
                Lines[firstPosition.Y] = line[0..firstPosition.X] + line[(lastPosition.X)..];
            }
            else
            {
                string firstLine = Lines[firstPosition.Y];
                string lastLine = Lines[lastPosition.Y];

                for (int i = firstPosition.Y + 1; i <= lastPosition.Y; i++)
                {
                    Lines.RemoveAt(firstPosition.Y + 1);
                }

                Lines[firstPosition.Y] = firstLine[0..firstPosition.X] + lastLine[(lastPosition.X)..];
            }

            _cursorPosition = firstPosition;
            CancelSelection();
        }

        private bool GetTextCursorPositionsInOrder(ref Point firstPosition, ref Point lastPosition)
        {
            firstPosition = _cursorPosition;
            lastPosition = _selectionStartPosition;

            bool bothCursorsOnSameLine = firstPosition.Y == lastPosition.Y;

            if (firstPosition.Y > lastPosition.Y || bothCursorsOnSameLine && firstPosition.X > lastPosition.X)
            {
                firstPosition = _selectionStartPosition;
                lastPosition = _cursorPosition;
            }

            return bothCursorsOnSameLine;
        }

        private Point GetCursorPositionFromMousePosition()
        {
            var x = Desktop.MouseX + _scrollingPixelsX - LayoutRectangle.X;
            var y = Desktop.MouseY + _scrollingPixelsY - LayoutRectangle.Y;

            int targetX = x / RendererHelper.FontRenderSize;
            int targetY = y / RendererHelper.FontRenderSize;

            bool wasRightHalfClicked = x % RendererHelper.FontRenderSize > RendererHelper.FontRenderSize / 2;
            if (wasRightHalfClicked) targetX++;

            targetY = Math.Clamp(targetY, 0, Lines.Count - 1);
            targetX = Math.Clamp(targetX, 0, Lines[targetY].Length);

            return new Point(targetX, targetY);
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
            EraseSelection();
            var line = Lines[_cursorPosition.Y];
            Lines[_cursorPosition.Y] = line[0.._cursorPosition.X] + text + line[_cursorPosition.X..];
            _cursorPosition.X += text.Length;

            CancelSelection();
            ClampScrolling();
        }

        private void CancelSelection()
        {
            _selectionStartPosition = _cursorPosition;
        }

        private bool HasSelection()
        {
            return _cursorPosition != _selectionStartPosition;
        }

        void ClampScrolling()
        {
            _scrollingPixelsY = Math.Clamp(_scrollingPixelsY,
                Math.Max(0, (_cursorPosition.Y + 1) * RendererHelper.FontRenderSize - LayoutRectangle.Height),
                Math.Max(0, Math.Min(_cursorPosition.Y * RendererHelper.FontRenderSize, Lines.Count * RendererHelper.FontRenderSize - LayoutRectangle.Height)));
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            var startY = _scrollingPixelsY / RendererHelper.FontRenderSize;
            var endY = Math.Min(Lines.Count - 1, (_scrollingPixelsY + LayoutRectangle.Height) / RendererHelper.FontRenderSize);

            var clipRect = Desktop.ToSDL_Rect(LayoutRectangle);
            SDL.SDL_RenderSetClipRect(Desktop.Renderer, ref clipRect);

            // Draw Selection
            var firstPosition = new Point();
            var lastPosition = new Point();
            bool bothCursorsOnSameLine = GetTextCursorPositionsInOrder(ref firstPosition, ref lastPosition);

            new Color(0x0000ffaa).UseAsDrawColor(Desktop.Renderer);
            if (Desktop.FocusedElement == this)
            {
                if (firstPosition != lastPosition)
                {
                    if (bothCursorsOnSameLine)
                    {
                        var selectionRect = Desktop.ToSDL_Rect(new DeepSwarmCommon.Rectangle(
                        LayoutRectangle.X + firstPosition.X * RendererHelper.FontRenderSize - _scrollingPixelsX,
                        LayoutRectangle.Y + firstPosition.Y * RendererHelper.FontRenderSize - _scrollingPixelsY,
                        (lastPosition.X - firstPosition.X) * RendererHelper.FontRenderSize, RendererHelper.FontRenderSize));
                        SDL.SDL_RenderFillRect(Desktop.Renderer, ref selectionRect);
                    }
                    else
                    {
                        var firstLineSelectionRect = Desktop.ToSDL_Rect(new DeepSwarmCommon.Rectangle(
                        LayoutRectangle.X + firstPosition.X * RendererHelper.FontRenderSize - _scrollingPixelsX,
                        LayoutRectangle.Y + firstPosition.Y * RendererHelper.FontRenderSize - _scrollingPixelsY,
                        (Lines[firstPosition.Y][firstPosition.X..].Length) * RendererHelper.FontRenderSize, RendererHelper.FontRenderSize));


                        var lastLineSelectionRect = Desktop.ToSDL_Rect(new DeepSwarmCommon.Rectangle(
                        LayoutRectangle.X + 0 - _scrollingPixelsX,
                        LayoutRectangle.Y + lastPosition.Y * RendererHelper.FontRenderSize - _scrollingPixelsY,
                        (Lines[lastPosition.Y][..lastPosition.X].Length) * RendererHelper.FontRenderSize, RendererHelper.FontRenderSize));

                        for (int i = firstPosition.Y + 1; i < lastPosition.Y; i++)
                        {
                            var midSelectionRect = Desktop.ToSDL_Rect(new DeepSwarmCommon.Rectangle(
                                LayoutRectangle.X + 0 - _scrollingPixelsX,
                                LayoutRectangle.Y + i * RendererHelper.FontRenderSize - _scrollingPixelsY,
                                (Lines[i].Length) * RendererHelper.FontRenderSize, RendererHelper.FontRenderSize));
                            SDL.SDL_RenderFillRect(Desktop.Renderer, ref midSelectionRect);
                        }

                        SDL.SDL_RenderFillRect(Desktop.Renderer, ref firstLineSelectionRect);
                        SDL.SDL_RenderFillRect(Desktop.Renderer, ref lastLineSelectionRect);
                    }
                }
            }

            // Draw Text
            for (var y = startY; y <= endY; y++)
            {
                RendererHelper.DrawText(Desktop.Renderer,
                    LayoutRectangle.X - _scrollingPixelsX,
                    LayoutRectangle.Y + y * RendererHelper.FontRenderSize - _scrollingPixelsY,
                    Lines[y], TextColor);
            }

            SDL.SDL_RenderSetClipRect(Desktop.Renderer, IntPtr.Zero);

            // Draw Text Cursor
            if (Desktop.FocusedElement == this)
            {
                new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);

                var cursorRect = Desktop.ToSDL_Rect(new DeepSwarmCommon.Rectangle(
                    LayoutRectangle.X + _cursorPosition.X * RendererHelper.FontRenderSize - _scrollingPixelsX,
                    LayoutRectangle.Y + _cursorPosition.Y * RendererHelper.FontRenderSize - _scrollingPixelsY,
                    2, RendererHelper.FontRenderSize));
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref cursorRect);
            }
        }

        public void SetText(string text)
        {
            Lines.Clear();
            Lines.AddRange(text.Replace("\r", "").Split("\n"));

            _cursorPosition = new Point();
        }

        public string GetText() => string.Join('\n', Lines);
    }
}
