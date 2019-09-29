using DeepSwarmCommon;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DeepSwarmClient.UI
{
    class Desktop
    {
        public IntPtr Renderer;

        public readonly Element RootElement;
        public Element FocusedElement { get; private set; }
        public Element HoveredElement { get; private set; }
        public bool IsHoveredElementPressed;

        public int MouseX { get; private set; }
        public int MouseY { get; private set; }

        // Animations
        readonly List<Action<float>> _animationActions = new List<Action<float>>();
        readonly List<Action<float>> _newAnimationActions = new List<Action<float>>();
        readonly List<Action<float>> _removedAnimationActions = new List<Action<float>>();

        bool _leftShiftDown = false;
        bool _rightShiftDown = false;

        bool _leftCtrlDown = false;
        bool _rightCtrlDown = false;

        public Desktop(IntPtr renderer)
        {
            Renderer = renderer;
            RootElement = new Element(this, null);
            RootElement.Mount();
        }

        #region Helpers
        public static SDL.SDL_Rect ToSDL_Rect(Rectangle rect) => new SDL.SDL_Rect { x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height };
        #endregion

        #region Configuration
        public void SetFocusedElement(Element element)
        {
            Debug.Assert(element == null || element.IsMounted);

            if (IsHoveredElementPressed)
            {
                SetHoveredElementPressed(false);
            }

            FocusedElement?.OnBlur();
            FocusedElement = element;
            FocusedElement?.OnFocus();
        }

        public void OnHoveredElementUnmounted()
        {
            HoveredElement?.OnMouseExit();
            HoveredElement = RootElement.HitTest(MouseX, MouseY);
            IsHoveredElementPressed = false;
        }

        public void SetHoveredElementPressed(bool pressed)
        {
            Debug.Assert(HoveredElement != null);
            IsHoveredElementPressed = pressed;

            if (!IsHoveredElementPressed) RefreshHoveredElement();
        }

        void RefreshHoveredElement()
        {
            Debug.Assert(!IsHoveredElementPressed);

            var hitElement = RootElement.HitTest(MouseX, MouseY);

            if (hitElement != HoveredElement)
            {
                HoveredElement?.OnMouseExit();
                HoveredElement = hitElement;
                HoveredElement?.OnMouseEnter();
            }
        }

        public void RegisterAnimation(Action<float> action)
        {
            if (_removedAnimationActions.Remove(action)) return;
            if (_animationActions.Contains(action) && !_removedAnimationActions.Contains(action)) throw new Exception("Cannot register same animation action twice");
            if (_newAnimationActions.Contains(action)) throw new Exception("Cannot register same animation action twice");
            _newAnimationActions.Add(action);
        }

        public void UnregisterAnimation(Action<float> action)
        {
            if (_newAnimationActions.Remove(action)) return;
            if (_removedAnimationActions.Contains(action)) throw new Exception("Cannot unregister same animatin action twice");
            if (!_animationActions.Contains(action)) throw new Exception("Cannot unregister action action that isn't registered");
            _removedAnimationActions.Add(action);
        }
        #endregion

        #region Lifecycle
        public void HandleSDLEvent(SDL.SDL_Event @event)
        {
            if (FocusedElement == null) return;

            switch (@event.type)
            {
                case SDL.SDL_EventType.SDL_KEYDOWN:
                    if (@event.key.keysym.sym == SDL.SDL_Keycode.SDLK_LSHIFT) _leftShiftDown = true;
                    if (@event.key.keysym.sym == SDL.SDL_Keycode.SDLK_RSHIFT) _rightShiftDown = true;
                    if (@event.key.keysym.sym == SDL.SDL_Keycode.SDLK_LCTRL) _leftCtrlDown = true;
                    if (@event.key.keysym.sym == SDL.SDL_Keycode.SDLK_RCTRL) _rightCtrlDown = true;

                    FocusedElement.OnKeyDown(@event.key.keysym.sym, repeat: @event.key.repeat != 0);
                    break;

                case SDL.SDL_EventType.SDL_KEYUP:
                    if (@event.key.keysym.sym == SDL.SDL_Keycode.SDLK_LSHIFT) _leftShiftDown = false;
                    if (@event.key.keysym.sym == SDL.SDL_Keycode.SDLK_RSHIFT) _rightShiftDown = false;
                    if (@event.key.keysym.sym == SDL.SDL_Keycode.SDLK_LCTRL) _leftCtrlDown = false;
                    if (@event.key.keysym.sym == SDL.SDL_Keycode.SDLK_RCTRL) _rightCtrlDown = false;

                    FocusedElement.OnKeyUp(@event.key.keysym.sym);
                    break;

                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    var textBytes = new byte[256];
                    string text;

                    unsafe
                    {
                        byte* endPtr = @event.text.text;
                        while (*endPtr != 0) endPtr++;
                        int length = (int)(endPtr - @event.text.text);
                        Marshal.Copy((IntPtr)@event.text.text, textBytes, 0, length);
                        text = Encoding.UTF8.GetString(textBytes, 0, length);
                    }

                    FocusedElement.OnTextEntered(text);
                    break;

                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    {
                        MouseX = @event.motion.x;
                        MouseY = @event.motion.y;

                        if (!IsHoveredElementPressed) RefreshHoveredElement();
                        HoveredElement?.OnMouseMove();
                        break;
                    }

                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    {
                        HoveredElement = IsHoveredElementPressed ? HoveredElement : RootElement.HitTest(@event.button.x, @event.button.y);
                        HoveredElement?.OnMouseDown(@event.button.button);
                        break;
                    }

                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    {
                        HoveredElement = IsHoveredElementPressed ? HoveredElement : RootElement.HitTest(@event.button.x, @event.button.y);
                        HoveredElement?.OnMouseUp(@event.button.button);
                        break;
                    }

                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    {
                        HoveredElement = IsHoveredElementPressed ? HoveredElement : RootElement.HitTest(MouseX, MouseY);
                        HoveredElement?.OnMouseWheel(@event.wheel.x, @event.wheel.y);
                        break;
                    }
            }
        }

        public void Animate(float deltaTime)
        {
            foreach (var removedAction in _removedAnimationActions) _animationActions.Remove(removedAction);
            _animationActions.AddRange(_newAnimationActions);
            _removedAnimationActions.Clear();
            _newAnimationActions.Clear();

            foreach (var action in _animationActions) action(deltaTime);
        }

        public bool IsShiftDown => _leftShiftDown || _rightShiftDown;

        public bool IsCtrlDown => _rightCtrlDown || _leftCtrlDown;

        public void Draw() => RootElement.Draw();
        #endregion
    }
}
