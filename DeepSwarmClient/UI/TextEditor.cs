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

        Point _startCursor = new Point();
        Point _endCursor = new Point();

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
                if (_startCursor.X > 0) _startCursor.X--;
                else if (_startCursor.Y > 0)
                {
                    _startCursor.Y--;
                    _startCursor.X = Lines[_startCursor.Y].Length;
                }

                ClampScrolling();
                SyncEndCursorToStart();
            }

            void GoRight()
            {
                if (_startCursor.X < Lines[_startCursor.Y].Length) _startCursor.X++;
                else if (_startCursor.Y < Lines.Count - 1)
                {
                    _startCursor.Y++;
                    _startCursor.X = 0;
                }

                ClampScrolling();
                SyncEndCursorToStart();
            }

            void GoUp()
            {
                if (_startCursor.Y > 0)
                {
                    _startCursor.Y--;
                    _startCursor.X = Math.Min(_startCursor.X, Lines[_startCursor.Y].Length);
                }
                else
                {
                    _startCursor.X = 0;
                }

                ClampScrolling();
                SyncEndCursorToStart();
            }

            void GoDown()
            {
                if (_startCursor.Y < Lines.Count - 1)
                {
                    _startCursor.Y++;
                    _startCursor.X = Math.Min(_startCursor.X, Lines[_startCursor.Y].Length);
                }
                else
                {
                    _startCursor.X = Lines[_startCursor.Y].Length;
                }

                ClampScrolling();
                SyncEndCursorToStart();
            }

            void Erase()
            {
                if (!HasSelection())
                {
                    var line = Lines[_startCursor.Y];

                    if (_startCursor.X > 0)
                    {
                        Lines[_startCursor.Y] = line[0..(_startCursor.X - 1)] + line[_startCursor.X..];
                        _startCursor.X--;
                    }
                    else if (_startCursor.Y > 0)
                    {
                        Lines.RemoveAt(_startCursor.Y);
                        _startCursor.Y--;
                        _startCursor.X = Lines[_startCursor.Y].Length;
                        Lines[_startCursor.Y] += line;
                    }
                }
                else
                {
                    EraseSelection();
                }

                ClampScrolling();
                SyncEndCursorToStart();
            }

            void Delete()
            {
                if (!HasSelection())
                {
                    var line = Lines[_startCursor.Y];

                    if (_startCursor.X < line.Length)
                    {
                        Lines[_startCursor.Y] = line[0.._startCursor.X] + line[(_startCursor.X + 1)..];
                    }
                    else if (_startCursor.Y < Lines.Count - 1)
                    {
                        Lines[_startCursor.Y] += Lines[_startCursor.Y + 1];
                        Lines.RemoveAt(_startCursor.Y + 1);
                    }
                }
                else
                {
                    EraseSelection();
                }

                ClampScrolling();
                SyncEndCursorToStart();
            }

            void BreakLine()
            {
                EraseSelection();
                if (_startCursor.X == 0)
                {
                    Lines.Insert(_startCursor.Y, "");
                    _startCursor.X = 0;
                    _startCursor.Y++;
                }
                else if (_startCursor.X == Lines[_startCursor.Y].Length)
                {
                    _startCursor.X = 0;
                    _startCursor.Y++;
                    Lines.Insert(_startCursor.Y, "");
                }
                else
                {
                    var endOfLine = Lines[_startCursor.Y][_startCursor.X..];
                    Lines[_startCursor.Y] = Lines[_startCursor.Y][0.._startCursor.X];
                    _startCursor.X = 0;
                    _startCursor.Y++;
                    Lines.Insert(_startCursor.Y, endOfLine);
                }

                ClampScrolling();
                SyncEndCursorToStart();
            }

            void GoToStartOfLine()
            {
                _startCursor.X = 0;
                SyncEndCursorToStart();
            }

            void GoToEndOfLine()
            {
                _startCursor.X = Lines[_startCursor.Y].Length;
                SyncEndCursorToStart();
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
                _startCursor = GetCursorPositionFromMousePosition();

                SyncEndCursorToStart();
            }
        }

        public override void OnMouseMove()
        {
            if (_isMakingSelection)
            {
                _endCursor = GetCursorPositionFromMousePosition();
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
            Point firstPosition = _startCursor;
            Point lastPosition = _endCursor;

            bool bothCursorsOnSameLine = firstPosition.Y == lastPosition.Y;

            if (firstPosition.Y > lastPosition.Y || bothCursorsOnSameLine && firstPosition.X > lastPosition.X)
            {
                firstPosition = _endCursor;
                lastPosition = _startCursor;
            }

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

            _startCursor = firstPosition;
            SyncEndCursorToStart();
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
            var line = Lines[_startCursor.Y];
            Lines[_startCursor.Y] = line[0.._startCursor.X] + text + line[_startCursor.X..];
            _startCursor.X += text.Length;

            SyncEndCursorToStart();
            ClampScrolling();
        }

        private void SyncEndCursorToStart()
        {
            _endCursor = _startCursor;
        }

        private bool HasSelection()
        {
            return _startCursor != _endCursor;
        }

        void ClampScrolling()
        {
            _scrollingPixelsY = Math.Clamp(_scrollingPixelsY,
                Math.Max(0, (_startCursor.Y + 1) * RendererHelper.FontRenderSize - LayoutRectangle.Height),
                Math.Max(0, Math.Min(_startCursor.Y * RendererHelper.FontRenderSize, Lines.Count * RendererHelper.FontRenderSize - LayoutRectangle.Height)));
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
                    LayoutRectangle.X + _startCursor.X * RendererHelper.FontRenderSize - _scrollingPixelsX,
                    LayoutRectangle.Y + _startCursor.Y * RendererHelper.FontRenderSize - _scrollingPixelsY,
                    2, RendererHelper.FontRenderSize));
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref cursorRect);

                new Color(0xff0000ff).UseAsDrawColor(Desktop.Renderer);

                var endCursorRect = Desktop.ToSDL_Rect(new DeepSwarmCommon.Rectangle(
                    LayoutRectangle.X + _endCursor.X * RendererHelper.FontRenderSize - _scrollingPixelsX,
                    LayoutRectangle.Y + _endCursor.Y * RendererHelper.FontRenderSize - _scrollingPixelsY,
                    2, RendererHelper.FontRenderSize));
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref endCursorRect);
            }
        }

        public void SetText(string text)
        {
            Lines.Clear();
            Lines.AddRange(text.Replace("\r", "").Split("\n"));

            _startCursor = new Point();
        }

        public string GetText() => string.Join('\n', Lines);
    }
}
