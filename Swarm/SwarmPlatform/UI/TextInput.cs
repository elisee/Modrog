using SDL2;
using SwarmBasics;
using SwarmBasics.Math;
using SwarmPlatform.Graphics;
using System;
using System.Text.RegularExpressions;

namespace SwarmPlatform.UI
{
    public class TextInput : Element
    {
        public FontStyle FontStyle;
        public Color TextColor = new Color(0xffffffff);
        public string Value { get; private set; } = "";
        public int MaxLength = byte.MaxValue;

        public Action OnChange;

        int _cursorX;
        int _selectionAnchorX;
        float _cursorTimer;
        public const float CursorFlashInterval = 1f;

        int _scrollingPixelsX;

        public TextInput(Element parent) : this(parent.Desktop, parent) { }
        public TextInput(Desktop desktop, Element parent) : base(desktop, parent)
        {
            FontStyle = Desktop.MainFontStyle;
        }

        #region Configuration
        public void SetValue(string value)
        {
            Value = value;
            _cursorX = _selectionAnchorX = value.Length;
        }


        public void SelectAll()
        {
            _selectionAnchorX = 0;
            _cursorX = Value.Length;
        }
        #endregion

        #region Internals
        protected override Point ComputeContentSize(int? maxWidth, int? maxHeight) => new Point(0, Height == null ? FontStyle.LineHeight : 0);

        public override bool AcceptsFocus() => !Disabled;

        void Animate(float deltaTime)
        {
            _cursorTimer += deltaTime;
        }

        int GetHoveredTextPosition()
        {
            var x = Desktop.MouseX + _scrollingPixelsX - _contentRectangle.X;

            var targetIndex = Value.Length;
            var textWidth = 0;

            for (var i = 0; i < Value.Length; i++)
            {
                var newTextWidth = FontStyle.MeasureText(Value[0..(i + 1)]);
                if (x < newTextWidth)
                {
                    targetIndex = i;

                    var wasRightHalfClicked = x - textWidth > (newTextWidth - textWidth) / 2;
                    if (wasRightHalfClicked) targetIndex++;

                    break;
                }

                textWidth = newTextWidth;
            }

            targetIndex = Math.Clamp(targetIndex, 0, Value.Length);

            return targetIndex;
        }

        enum CharType { Word, Space, Other }
        static readonly Regex WordRegex = new Regex(@"^\w");
        static readonly Regex SpacesRegex = new Regex(@"^[ ]");
        static readonly Regex OthersRegex = new Regex(@"^[^\w ]");

        int GetStartOfWord(int positionX, Regex regex)
        {
            var startOfWord = positionX;

            for (var i = startOfWord; i > 0; i--)
            {
                if (!regex.IsMatch(Value[i - 1].ToString())) break;
                startOfWord--;
            }

            return startOfWord;
        }

        int GetEndOfWord(int positionX, Regex regex)
        {
            var endOfWord = positionX;

            for (var i = endOfWord + 1; i <= Value.Length; i++)
            {
                if (!regex.IsMatch(Value[i - 1].ToString())) break;
                endOfWord++;
            }

            return endOfWord;
        }

        void GetSelectionRange(out int start, out int end)
        {
            start = _selectionAnchorX;
            end = _cursorX;

            if (start > end)
            {
                start = _cursorX;
                end = _selectionAnchorX;
            }
        }

        void ClearSelection() { _selectionAnchorX = _cursorX; _cursorTimer = 0f; }

        bool HasSelection() => _cursorX != _selectionAnchorX;

        void EraseSelection()
        {
            GetSelectionRange(out var selectionStart, out var selectionEnd);
            Value = Value[0..selectionStart] + Value[selectionEnd..];
            _cursorX = _selectionAnchorX = selectionStart;
        }

        void ClampScrolling()
        {
            var cursorPixelsX = FontStyle.MeasureText(Value[0.._cursorX]);
            var textWidth = FontStyle.MeasureText(Value);

            _scrollingPixelsX = Math.Clamp(_scrollingPixelsX,
                Math.Max(0, cursorPixelsX - _contentRectangle.Width),
                Math.Max(0, Math.Min(cursorPixelsX, textWidth - _contentRectangle.Width)));
        }
        #endregion

        #region Events
        public override Element HitTest(int x, int y) => LayoutRectangle.Contains(x, y) ? this : null;

        public override void OnMouseEnter() => SDL.SDL_SetCursor(Cursors.IBeamCursor);
        public override void OnMouseExit() => SDL.SDL_SetCursor(Cursors.ArrowCursor);

        public override void OnFocus() { _cursorTimer = 0f; Desktop.RegisterAnimation(Animate); }
        public override void OnBlur() => Desktop.UnregisterAnimation(Animate);

        public override void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            switch (key)
            {
                // Navigate
                case SDL.SDL_Keycode.SDLK_LEFT: GoLeft(); break;
                case SDL.SDL_Keycode.SDLK_RIGHT: GoRight(); break;
                case SDL.SDL_Keycode.SDLK_HOME: GoToStartOfLine(); break;
                case SDL.SDL_Keycode.SDLK_END: GoToEndOfLine(); break;
                case SDL.SDL_Keycode.SDLK_a: if (Desktop.HasControlKeyModifierAlone) SelectAll(); break;

                // Edit
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

                if (Desktop.HasControlKeyModifier)
                {
                    var sourceChar = _cursorX >= 0 && _cursorX < Value.Length ? Value[_cursorX].ToString() : " ";
                    if (sourceChar == "." || sourceChar == " ") sourceChar = _cursorX - 1 >= 0 && _cursorX - 1 < Value.Length ? Value[_cursorX - 1].ToString() : " ";

                    var regex = WordRegex.IsMatch(sourceChar) ? WordRegex : (SpacesRegex.IsMatch(sourceChar) ? SpacesRegex : OthersRegex);
                    _cursorX = GetStartOfWord(_cursorX, regex);
                }
                ClearSelectionUnlessShiftDown();
            }

            void GoRight()
            {
                if (_cursorX < Value.Length)
                {
                    _cursorX++;
                    ClampScrolling();
                }

                if (Desktop.HasControlKeyModifier)
                {
                    var sourceChar = _cursorX - 1 >= 0 && _cursorX - 1 < Value.Length ? Value[_cursorX - 1].ToString() : " ";
                    if (sourceChar == "." || sourceChar == " ") sourceChar = _cursorX >= 0 && _cursorX < Value.Length ? Value[_cursorX].ToString() : " ";

                    var regex = WordRegex.IsMatch(sourceChar) ? WordRegex : (SpacesRegex.IsMatch(sourceChar) ? SpacesRegex : OthersRegex);
                    _cursorX = GetEndOfWord(_cursorX, regex);
                }
                ClearSelectionUnlessShiftDown();
            }

            void Erase()
            {
                if (!HasSelection())
                {
                    if (_cursorX > 0)
                    {
                        Value = Value[0..(_cursorX - 1)] + Value[_cursorX..];
                        _cursorX--;
                    }
                }
                else
                {
                    EraseSelection();
                }

                ClampScrolling();
                ClearSelection();

                OnChange?.Invoke();
            }

            void Delete()
            {
                if (!HasSelection())
                {
                    if (_cursorX < Value.Length)
                    {
                        Value = Value[0.._cursorX] + Value[(_cursorX + 1)..];
                    }
                }
                else
                {
                    EraseSelection();
                }

                ClampScrolling();
                ClearSelection();

                OnChange?.Invoke();
            }

            void GoToStartOfLine()
            {
                _cursorX = 0;
                ClearSelectionUnlessShiftDown();
            }

            void GoToEndOfLine()
            {
                _cursorX = Value.Length;
                ClearSelectionUnlessShiftDown();
            }

            void ClearSelectionUnlessShiftDown()
            {
                if (!Desktop.HasShiftKeyModifier) ClearSelection();
                _cursorTimer = 0f;
            }

            void SelectAll()
            {
                _selectionAnchorX = 0;
                _cursorX = Value.Length;
                ClampScrolling();
            }
        }

        public override void OnTextEntered(string text)
        {
            EraseSelection();
            if (Value.Length >= MaxLength) return;
            if (Value.Length + text.Length > MaxLength) text = text[0..(MaxLength - Value.Length)];

            Value = Value[0.._cursorX] + text + Value[_cursorX..];
            _cursorX += text.Length;
            ClampScrolling();
            ClearSelection();

            OnChange?.Invoke();
        }

        public override void OnMouseDown(int button, int clicks)
        {
            if (button == SDL.SDL_BUTTON_LEFT)
            {
                Desktop.SetFocusedElement(this);
                Desktop.SetHoveredElementPressed(true);
                _cursorX = _selectionAnchorX = GetHoveredTextPosition();
                ClampScrolling();

                if (clicks == 2)
                {
                    var leftChar = _cursorX - 1 >= 0 && _cursorX - 1 < Value.Length ? Value[_cursorX - 1].ToString() : " ";
                    var rightChar = _cursorX < Value.Length ? Value[_cursorX].ToString() : " ";

                    var leftCharType = WordRegex.IsMatch(leftChar) ? CharType.Word : (SpacesRegex.IsMatch(leftChar) ? CharType.Space : CharType.Other);
                    var rightCharType = WordRegex.IsMatch(rightChar) ? CharType.Word : (SpacesRegex.IsMatch(rightChar) ? CharType.Space : CharType.Other);
                    if (leftCharType == CharType.Word || rightCharType == CharType.Word)
                    {
                        if (leftCharType == CharType.Word) _selectionAnchorX = GetStartOfWord(_selectionAnchorX, WordRegex);
                        if (rightCharType == CharType.Word) _cursorX = GetEndOfWord(_cursorX, WordRegex);
                    }
                    else if (leftCharType == CharType.Space && rightCharType == CharType.Space)
                    {
                        _selectionAnchorX = GetStartOfWord(_selectionAnchorX, SpacesRegex);
                        _cursorX = GetEndOfWord(_cursorX, SpacesRegex);
                    }
                    else
                    {
                        if (leftCharType == CharType.Other) _selectionAnchorX = GetStartOfWord(_selectionAnchorX, OthersRegex);
                        if (rightCharType == CharType.Other) _cursorX = GetEndOfWord(_cursorX, OthersRegex);
                    }
                }
                else if (clicks == 3)
                {
                    _selectionAnchorX = 0;
                    _cursorX = Value.Length;
                }
            }
        }

        public override void OnMouseMove()
        {
            if (IsPressed)
            {
                _cursorX = GetHoveredTextPosition();
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
        #endregion

        #region Drawing
        protected override void DrawSelf()
        {
            base.DrawSelf();

            var clipRect = _contentRectangle.ToSDL_Rect();
            SDL.SDL_RenderSetClipRect(Desktop.Renderer, ref clipRect);

            // Draw selection
            if (HasSelection())
            {
                new Color(0x0000ffaa).UseAsDrawColor(Desktop.Renderer);
                GetSelectionRange(out var firstX, out var lastX);

                var firstPixelsX = FontStyle.MeasureText(Value[0..firstX]);
                var lastPixelsX = FontStyle.MeasureText(Value[0..lastX]);

                var selectionRect = new Rectangle(
                    _contentRectangle.X + firstPixelsX - _scrollingPixelsX, _contentRectangle.Y,
                    lastPixelsX - firstPixelsX, FontStyle.LineHeight).ToSDL_Rect();
                SDL.SDL_RenderFillRect(Desktop.Renderer, ref selectionRect);
            }

            TextColor.UseAsDrawColor(Desktop.Renderer);
            FontStyle.DrawText(_contentRectangle.X - _scrollingPixelsX, _contentRectangle.Y + _contentRectangle.Height / 2 - FontStyle.Size / 2, Value);

            SDL.SDL_RenderSetClipRect(Desktop.Renderer, IntPtr.Zero);

            // Draw cursor
            if (Desktop.FocusedElement == this && (_cursorTimer % CursorFlashInterval * 2) < CursorFlashInterval)
            {
                new Color(0xffffffff).UseAsDrawColor(Desktop.Renderer);

                var cursorRect = new Rectangle(
                    _contentRectangle.X + FontStyle.MeasureText(Value[0.._cursorX]) - _scrollingPixelsX,
                    _contentRectangle.Y,
                    2, _contentRectangle.Height).ToSDL_Rect();
                SDL.SDL_RenderDrawRect(Desktop.Renderer, ref cursorRect);
            }
        }
        #endregion
    }
}
