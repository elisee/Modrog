﻿using SDL2;
using SwarmBasics;
using SwarmBasics.Math;
using SwarmPlatform.Graphics;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SwarmPlatform.UI
{

    public class TextEditor : Element
    {
        public FontStyle FontStyle;
        public Color TextColor = new Color(0xffffffff);
        public Point CellSize = new Point(12, 32);

        public Action OnChange;

        public readonly List<string> Lines = new List<string> { "" };
        public const int CursorWidth = 2;

        Point _cursor;
        Point _selectionAnchor;
        float _cursorTimer;

        public TextEditor(Element parent) : this(parent.Desktop, parent) { }
        public TextEditor(Desktop desktop, Element parent) : base(desktop, parent)
        {
            FontStyle = Desktop.MonoFontStyle;
            Flow = Flow.Scroll;
            _scrollMultiplier = CellSize.Y * 3;
        }

        #region Configuration
        public void SetText(string text)
        {
            Lines.Clear();
            Lines.AddRange(text.Replace("\r", "").Split('\n'));
            _cursor = _selectionAnchor = Point.Zero;
        }

        public string GetText() => string.Join('\n', Lines);
        #endregion

        #region Internals
        protected override Point ComputeContentSize(int? maxWidth, int? maxHeight)
        {
            var contentSize = new Point(0, Lines.Count * CellSize.Y);
            foreach (var line in Lines) contentSize.X = Math.Max(contentSize.X, line.Length * CellSize.X);
            contentSize.X += CursorWidth;

            return contentSize;
        }

        public override bool AcceptsFocus() => !Disabled;

        public override Element HitTest(int x, int y) => LayoutRectangle.Contains(x, y) ? this : null;
        public override void LayoutSelf() => ScrollCursorIntoView();

        void Animate(float deltaTime)
        {
            _cursorTimer += deltaTime;
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

        void EraseSelection()
        {
            GetSelectionRange(out var start, out var end);

            if (start.Y == end.Y)
            {
                Lines[start.Y] = Lines[start.Y][0..start.X] + Lines[start.Y][end.X..];
            }
            else
            {
                var firstLine = Lines[start.Y];
                var lastLine = Lines[end.Y];
                for (var i = start.Y + 1; i <= end.Y; i++) Lines.RemoveAt(start.Y + 1);

                Lines[start.Y] = firstLine[0..start.X] + lastLine[end.X..];
            }

            _cursor = _selectionAnchor = start;
        }

        Point GetHoveredTextPosition()
        {
            var x = Desktop.MouseX + _contentScroll.X - ViewRectangle.X;
            var y = Desktop.MouseY + _contentScroll.Y - ViewRectangle.Y;

            var targetX = x / CellSize.X;
            var targetY = y / CellSize.Y;

            var wasRightHalfClicked = x % CellSize.X > CellSize.X / 2;
            if (wasRightHalfClicked) targetX++;

            targetY = Math.Clamp(targetY, 0, Lines.Count - 1);
            targetX = Math.Clamp(targetX, 0, Lines[targetY].Length);

            return new Point(targetX, targetY);
        }

        enum CharType { Word, Space, Other }
        static readonly Regex WordRegex = new Regex(@"^\w");
        static readonly Regex SpacesRegex = new Regex(@"^[ ]");
        static readonly Regex OthersRegex = new Regex(@"^[^\w ]");

        int GetStartOfWord(Point position, Regex regex)
        {
            var startOfWord = position.X;
            var line = Lines[position.Y];

            for (var i = startOfWord; i > 0; i--)
            {
                if (!regex.IsMatch(line[i - 1].ToString())) break;
                startOfWord--;
            }

            return startOfWord;
        }

        int GetEndOfWord(Point position, Regex regex)
        {
            var endOfWord = position.X;
            var line = Lines[position.Y];

            for (var i = endOfWord + 1; i <= line.Length; i++)
            {
                if (!regex.IsMatch(line[i - 1].ToString())) break;
                endOfWord++;
            }

            return endOfWord;
        }

        void ScrollCursorIntoView()
        {
            _cursorTimer = 0f;
            ScrollIntoView(_cursor.X * CellSize.X, _cursor.Y * CellSize.Y);
            ScrollIntoView(_cursor.X * CellSize.X + CursorWidth, (_cursor.Y + 1) * CellSize.Y);
        }
        #endregion

        #region Events
        public override void OnMouseEnter() => SDL.SDL_SetCursor(Cursors.IBeamCursor);
        public override void OnMouseExit() => SDL.SDL_SetCursor(Cursors.ArrowCursor);

        public override void OnFocus() { _cursorTimer = 0f; Desktop.RegisterAnimation(Animate); }
        public override void OnBlur() { Desktop.UnregisterAnimation(Animate); }

        public override void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            switch (key)
            {
                // Navigate
                case SDL.SDL_Keycode.SDLK_LEFT: GoLeft(); return;
                case SDL.SDL_Keycode.SDLK_RIGHT: GoRight(); return;
                case SDL.SDL_Keycode.SDLK_UP: GoUp(1); return;
                case SDL.SDL_Keycode.SDLK_DOWN: GoDown(1); return;
                case SDL.SDL_Keycode.SDLK_HOME: GoToStartOfLine(); return;
                case SDL.SDL_Keycode.SDLK_END: GoToEndOfLine(); return;
                case SDL.SDL_Keycode.SDLK_PAGEUP: GoUp((int)MathF.Ceiling((float)ViewRectangle.Height / CellSize.Y) - 2); return;
                case SDL.SDL_Keycode.SDLK_PAGEDOWN: GoDown((int)MathF.Ceiling((float)ViewRectangle.Height / CellSize.Y) - 2); return;

                case SDL.SDL_Keycode.SDLK_a:
                    if (Desktop.IsCtrlOnlyDown)
                    {
                        SelectAll();
                        return;
                    }
                    break;

                // Edit
                case SDL.SDL_Keycode.SDLK_BACKSPACE: Erase(); OnChange?.Invoke(); return;
                case SDL.SDL_Keycode.SDLK_DELETE: Delete(); OnChange?.Invoke(); return;

                case SDL.SDL_Keycode.SDLK_TAB:
                    if (Desktop.HasNoKeyModifier || Desktop.IsShiftOnlyDown)
                    {
                        Indent();
                        OnChange?.Invoke();
                        return;
                    }
                    break;

                case SDL.SDL_Keycode.SDLK_RETURN:
                case SDL.SDL_Keycode.SDLK_KP_ENTER:
                    if (Desktop.HasNoKeyModifier)
                    {
                        BreakLine();
                        OnChange?.Invoke();
                        return;
                    }
                    break;

                case SDL.SDL_Keycode.SDLK_x:
                case SDL.SDL_Keycode.SDLK_c:
                case SDL.SDL_Keycode.SDLK_v:
                    if (Desktop.IsCtrlOnlyDown)
                    {
                        switch (key)
                        {
                            case SDL.SDL_Keycode.SDLK_x: Cut(); break;
                            case SDL.SDL_Keycode.SDLK_c: Copy(); break;
                            case SDL.SDL_Keycode.SDLK_v: Paste(); break;
                        }

                        OnChange?.Invoke();
                        return;
                    }

                    break;
            }

            base.OnKeyDown(key, repeat);

            #region Navigate
            void GoLeft()
            {
                if (_cursor.X > 0)
                {
                    _cursor.X--;
                }
                else if (_cursor.Y > 0)
                {
                    _cursor.Y--;
                    _cursor.X = Lines[_cursor.Y].Length;
                }

                if (Desktop.IsCtrlDown)
                {
                    var line = Lines[_cursor.Y];
                    var sourceChar = _cursor.X >= 0 && _cursor.X < line.Length ? line[_cursor.X].ToString() : " ";
                    if (sourceChar == "." || sourceChar == " ") sourceChar = _cursor.X - 1 >= 0 && _cursor.X - 1 < line.Length ? line[_cursor.X - 1].ToString() : " ";

                    var regex = WordRegex.IsMatch(sourceChar) ? WordRegex : (SpacesRegex.IsMatch(sourceChar) ? SpacesRegex : OthersRegex);
                    _cursor.X = GetStartOfWord(_cursor, regex);
                }
                if (!Desktop.IsShiftDown) _selectionAnchor = _cursor;

                ScrollCursorIntoView();
            }

            void GoRight()
            {
                if (_cursor.X < Lines[_cursor.Y].Length)
                {
                    _cursor.X++;
                }
                else if (_cursor.Y < Lines.Count - 1)
                {
                    _cursor.Y++;
                    _cursor.X = 0;
                }

                if (Desktop.IsCtrlDown)
                {
                    var line = Lines[_cursor.Y];
                    var sourceChar = _cursor.X - 1 >= 0 && _cursor.X - 1 < line.Length ? line[_cursor.X - 1].ToString() : " ";
                    if (sourceChar == "." || sourceChar == " ") sourceChar = _cursor.X >= 0 && _cursor.X < line.Length ? line[_cursor.X].ToString() : " ";

                    var regex = WordRegex.IsMatch(sourceChar) ? WordRegex : (SpacesRegex.IsMatch(sourceChar) ? SpacesRegex : OthersRegex);
                    _cursor.X = GetEndOfWord(_cursor, regex);
                }
                if (!Desktop.IsShiftDown) _selectionAnchor = _cursor;

                ScrollCursorIntoView();
            }

            void GoUp(int lineCount)
            {
                if (_cursor.Y - lineCount >= 0)
                {
                    _cursor.Y -= lineCount;
                    _cursor.X = Math.Min(_cursor.X, Lines[_cursor.Y].Length);
                }
                else
                {
                    _cursor.Y = 0;
                    _cursor.X = 0;
                }

                if (!Desktop.IsShiftDown) _selectionAnchor = _cursor;

                ScrollCursorIntoView();
            }

            void GoDown(int lineCount)
            {
                if (_cursor.Y + lineCount < Lines.Count)
                {
                    _cursor.Y += lineCount;
                    _cursor.X = Math.Min(_cursor.X, Lines[_cursor.Y].Length);
                }
                else
                {
                    _cursor.Y = Lines.Count - 1;
                    _cursor.X = Lines[_cursor.Y].Length;
                }

                if (!Desktop.IsShiftDown) _selectionAnchor = _cursor;

                ScrollCursorIntoView();
            }

            void GoToStartOfLine()
            {
                _cursor.X = 0;
                if (!Desktop.IsShiftDown) _selectionAnchor = _cursor;

                ScrollCursorIntoView();
            }

            void GoToEndOfLine()
            {
                _cursor.X = Lines[_cursor.Y].Length;
                if (!Desktop.IsShiftDown) _selectionAnchor = _cursor;

                ScrollCursorIntoView();
            }

            void SelectAll()
            {
                _selectionAnchor = Point.Zero;
                _cursor.Y = Lines.Count - 1;
                _cursor.X = Lines[_cursor.Y].Length;

                ScrollCursorIntoView();
            }
            #endregion

            #region Edit
            void Erase()
            {
                if (_selectionAnchor == _cursor)
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

                    _selectionAnchor = _cursor;
                }
                else
                {
                    EraseSelection();
                }
                Layout();
            }

            void Delete()
            {
                if (_selectionAnchor == _cursor)
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

                Layout();
            }

            void Internal_BreakLine()
            {
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
            }

            void BreakLine()
            {
                EraseSelection();
                Internal_BreakLine();

                _selectionAnchor = _cursor;
                Layout();
            }

            void Indent()
            {
                const int TabSpaceCount = 4;

                var startY = Math.Min(_cursor.Y, _selectionAnchor.Y);
                var endY = Math.Max(_cursor.Y, _selectionAnchor.Y);

                if (startY == endY)
                {
                    var startX = Math.Min(_cursor.X, _selectionAnchor.X);

                    if (!Desktop.IsShiftDown)
                    {
                        var spacesToInsert = TabSpaceCount - startX % TabSpaceCount;

                        Lines[_cursor.Y] = Lines[_cursor.Y][0..startX] + new string(' ', spacesToInsert) + Lines[_cursor.Y][startX..];
                        _cursor.X += spacesToInsert;
                        _selectionAnchor.X += spacesToInsert;
                    }
                    else
                    {
                        var maxSpacesToRemove = startX % TabSpaceCount;
                        if (maxSpacesToRemove == 0) maxSpacesToRemove = TabSpaceCount;

                        var spacesToRemove = 0;
                        for (var i = 0; i < maxSpacesToRemove; i++)
                        {
                            var newStartX = startX - (i + 1);
                            if (newStartX < 0 || Lines[_cursor.Y][newStartX] != ' ') break;
                            spacesToRemove++;
                        }

                        Lines[_cursor.Y] = Lines[_cursor.Y][0..(startX - spacesToRemove)] + Lines[_cursor.Y][startX..];

                        _cursor.X -= spacesToRemove;
                        _selectionAnchor.X -= spacesToRemove;
                    }
                }
                else
                {
                    if (!Desktop.IsShiftDown)
                    {
                        var insert = new string(' ', TabSpaceCount);
                        for (var y = startY; y <= endY; y++)
                        {
                            if (Lines[y].Length == 0) continue;
                            Lines[y] = insert + Lines[y];

                            if (y == _cursor.Y && _cursor.X > 0) _cursor.X += TabSpaceCount;
                            if (y == _selectionAnchor.Y && _selectionAnchor.X > 0) _selectionAnchor.X += TabSpaceCount;
                        }
                    }
                    else
                    {
                        for (var y = startY; y <= endY; y++)
                        {
                            var spacesToRemove = 0;

                            for (var i = 0; i < TabSpaceCount; i++)
                            {
                                if (i >= Lines[y].Length || Lines[y][i] != ' ') break;
                                spacesToRemove++;
                            }

                            Lines[y] = Lines[y][spacesToRemove..];

                            if (y == _cursor.Y) _cursor.X = Math.Max(0, _cursor.X - spacesToRemove);
                            if (y == _selectionAnchor.Y) _selectionAnchor.X = Math.Max(0, _selectionAnchor.X - spacesToRemove);
                        }
                    }
                }

                Layout();
            }

            string GetSelectionText()
            {
                GetSelectionRange(out var start, out var end);

                if (start.Y == end.Y) return Lines[start.Y][start.X..end.X];

                var text = Lines[start.Y][start.X..] + '\n';
                for (var i = start.Y + 1; i < end.Y; i++) text += Lines[i] + '\n';
                text += Lines[end.Y][0..end.X];

                return text;
            }

            void Cut()
            {
                SDL.SDL_SetClipboardText(GetSelectionText());
                EraseSelection();
                Layout();
            }

            void Copy()
            {
                SDL.SDL_SetClipboardText(GetSelectionText());
            }

            void Paste()
            {
                EraseSelection();

                var pastedLines = SDL.SDL_GetClipboardText().Replace("\r", "").Split('\n');
                Lines[_cursor.Y] = Lines[_cursor.Y][0.._cursor.X] + pastedLines[0] + Lines[_cursor.Y][_cursor.X..];
                _cursor.X += pastedLines[0].Length;

                if (pastedLines.Length > 1)
                {
                    Internal_BreakLine();

                    if (pastedLines.Length > 2)
                    {
                        Lines.InsertRange(_cursor.Y, pastedLines[1..^1]);
                        _cursor.Y += pastedLines.Length - 2;
                    }

                    Lines[_cursor.Y] = pastedLines[^1] + Lines[_cursor.Y];
                    _cursor.X = pastedLines[^1].Length;
                }

                _selectionAnchor = _cursor;
                Layout();

            }
            #endregion
        }

        public override void OnTextEntered(string text)
        {
            EraseSelection();

            Lines[_cursor.Y] = Lines[_cursor.Y][0.._cursor.X] + text + Lines[_cursor.Y][_cursor.X..];
            _cursor.X = _selectionAnchor.X = _cursor.X + text.Length;

            ScrollCursorIntoView();
            Layout();

            OnChange?.Invoke();
        }

        public override void OnMouseDown(int button, int clicks)
        {
            if (button == SDL.SDL_BUTTON_LEFT)
            {
                Desktop.SetFocusedElement(this);
                Desktop.SetHoveredElementPressed(true);

                _cursor = _selectionAnchor = GetHoveredTextPosition();

                if (clicks == 2)
                {
                    var line = Lines[_cursor.Y];
                    var leftChar = _cursor.X - 1 >= 0 && _cursor.X - 1 < line.Length ? line[_cursor.X - 1].ToString() : " ";
                    var rightChar = _cursor.X < line.Length ? line[_cursor.X].ToString() : " ";

                    var leftCharType = WordRegex.IsMatch(leftChar) ? CharType.Word : (SpacesRegex.IsMatch(leftChar) ? CharType.Space : CharType.Other);
                    var rightCharType = WordRegex.IsMatch(rightChar) ? CharType.Word : (SpacesRegex.IsMatch(rightChar) ? CharType.Space : CharType.Other);
                    if (leftCharType == CharType.Word || rightCharType == CharType.Word)
                    {
                        if (leftCharType == CharType.Word) _selectionAnchor.X = GetStartOfWord(_selectionAnchor, WordRegex);
                        if (rightCharType == CharType.Word) _cursor.X = GetEndOfWord(_cursor, WordRegex);
                    }
                    else if (leftCharType == CharType.Space && rightCharType == CharType.Space)
                    {
                        _selectionAnchor.X = GetStartOfWord(_selectionAnchor, SpacesRegex);
                        _cursor.X = GetEndOfWord(_cursor, SpacesRegex);
                    }
                    else
                    {
                        if (leftCharType == CharType.Other) _selectionAnchor.X = GetStartOfWord(_selectionAnchor, OthersRegex);
                        if (rightCharType == CharType.Other) _cursor.X = GetEndOfWord(_cursor, OthersRegex);
                    }
                }
                else if (clicks == 3)
                {
                    _selectionAnchor.X = 0;
                    _cursor.X = Lines[_cursor.Y].Length;
                }

                ScrollCursorIntoView();
            }
        }

        public override void OnMouseMove()
        {
            if (IsPressed)
            {
                _cursor = GetHoveredTextPosition();
                ScrollCursorIntoView();
            }
        }

        public override void OnMouseUp(int button)
        {
            if (button == SDL.SDL_BUTTON_LEFT && IsPressed)
            {
                Desktop.SetHoveredElementPressed(false);
                ScrollCursorIntoView();
            }
        }
        #endregion

        #region Drawing
        protected override void DrawSelf()
        {
            base.DrawSelf();

            var startY = _contentScroll.Y / CellSize.Y;
            var endY = Math.Min(Lines.Count - 1, (_contentScroll.Y + _contentRectangle.Height) / CellSize.Y);

            Desktop.PushClipRect(ViewRectangle);

            // Draw selection
            if (_selectionAnchor != _cursor)
            {
                new Color(0x0000ffaa).UseAsDrawColor(Desktop.Renderer);
                GetSelectionRange(out var firstPosition, out var lastPosition);

                if (firstPosition.Y == lastPosition.Y)
                {
                    var selectionRect = new Rectangle(
                        _contentRectangle.X + firstPosition.X * CellSize.X,
                        _contentRectangle.Y + firstPosition.Y * CellSize.Y,
                        (lastPosition.X - firstPosition.X) * CellSize.X, CellSize.Y).ToSDL_Rect();

                    SDL.SDL_RenderFillRect(Desktop.Renderer, ref selectionRect);
                }
                else
                {
                    if (firstPosition.Y >= startY)
                    {
                        var firstRect = new Rectangle(
                            _contentRectangle.X + firstPosition.X * CellSize.X,
                            _contentRectangle.Y + firstPosition.Y * CellSize.Y,
                            Math.Max(1, Lines[firstPosition.Y][firstPosition.X..].Length) * CellSize.X, CellSize.Y).ToSDL_Rect();

                        SDL.SDL_RenderFillRect(Desktop.Renderer, ref firstRect);
                    }

                    for (int i = Math.Max(startY, firstPosition.Y + 1); i < Math.Min(endY, lastPosition.Y); i++)
                    {
                        var midRect = new Rectangle(
                            _contentRectangle.X,
                            _contentRectangle.Y + i * CellSize.Y,
                            Math.Max(1, Lines[i].Length) * CellSize.X, CellSize.Y).ToSDL_Rect();

                        SDL.SDL_RenderFillRect(Desktop.Renderer, ref midRect);
                    }

                    if (lastPosition.Y <= endY)
                    {
                        var lastRect = new Rectangle(
                            _contentRectangle.X,
                            _contentRectangle.Y + lastPosition.Y * CellSize.Y,
                            (Lines[lastPosition.Y][..lastPosition.X].Length) * CellSize.X, CellSize.Y).ToSDL_Rect();

                        SDL.SDL_RenderFillRect(Desktop.Renderer, ref lastRect);
                    }
                }
            }

            // Draw text
            TextColor.UseAsDrawColor(Desktop.Renderer);
            for (var y = startY; y <= endY; y++)
            {
                FontStyle.DrawText(
                    _contentRectangle.X,
                    _contentRectangle.Y + y * CellSize.Y + (CellSize.Y / 2 - FontStyle.LineSpacing / 2),
                    Lines[y]);
            }

            // Draw cursor
            if (Desktop.FocusedElement == this &&
                (_cursorTimer % TextInput.CursorFlashInterval * 2) < TextInput.CursorFlashInterval &&
                _cursor.Y >= startY && _cursor.Y <= endY)
            {
                new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);

                var cursorRect = new Rectangle(
                    _contentRectangle.X + _cursor.X * CellSize.X,
                    _contentRectangle.Y + _cursor.Y * CellSize.Y,
                    CursorWidth, CellSize.Y).ToSDL_Rect();
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref cursorRect);
            }

            Desktop.PopClipRect();
        }
        #endregion
    }
}
