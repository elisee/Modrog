using DeepSwarmCommon;
using SDL2;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DeepSwarmClient.UI
{
    class Desktop
    {
        public IntPtr Renderer;

        public Element RootElement { get; private set; }
        public Element FocusedElement;
        public Element HoveredElement;

        public int MouseX { get; private set; }
        public int MouseY { get; private set; }

        // Animations
        readonly List<Action<float>> _animationActions = new List<Action<float>>();
        readonly List<Action<float>> _newAnimationActions = new List<Action<float>>();
        readonly List<Action<float>> _removedAnimationActions = new List<Action<float>>();

        public Desktop(IntPtr renderer)
        {
            Renderer = renderer;
        }

        #region Helpers
        public static SDL.SDL_Rect ToSDL_Rect(Rectangle rect) => new SDL.SDL_Rect { x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height };
        #endregion

        #region Configuration
        public void SetRootElement(Element element)
        {
            RootElement?.Unmount();
            RootElement = element;
            RootElement?.Mount();
            RootElement.Layout(new Rectangle(0, 0, 1280, 720));
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
                    FocusedElement.OnKeyDown(@event.key.keysym.sym, repeat: @event.key.repeat != 0);
                    break;

                case SDL.SDL_EventType.SDL_KEYUP:
                    FocusedElement.OnKeyUp(@event.key.keysym.sym);
                    break;

                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    MouseX = @event.motion.x;
                    MouseY = @event.motion.y;

                    {
                        var hitElement = RootElement.HitTest(@event.motion.x, @event.motion.y);

                        if (hitElement != HoveredElement)
                        {
                            HoveredElement?.OnMouseExit();
                            hitElement?.OnMouseEnter();
                            HoveredElement = hitElement;
                        }

                        hitElement?.OnMouseMove();
                    }
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    {
                        var hitElement = RootElement.HitTest(@event.button.x, @event.button.y);
                        hitElement?.OnMouseDown(@event.button.button);
                    }
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    {
                        var hitElement = RootElement.HitTest(@event.button.x, @event.button.y);
                        hitElement?.OnMouseUp(@event.button.button);
                    }
                    break;

                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    {
                        var hitElement = RootElement.HitTest(MouseX, MouseY);
                        hitElement?.OnMouseWheel(@event.wheel.x, @event.wheel.y);
                    }
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

        public void Draw() => RootElement.Draw();
        #endregion
    }
}
