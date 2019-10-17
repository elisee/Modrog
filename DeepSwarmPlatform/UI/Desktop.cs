using DeepSwarmBasics.Math;
using DeepSwarmPlatform.Graphics;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DeepSwarmPlatform.UI
{
    public class Desktop
    {
        public IntPtr Renderer;
        public FontStyle MainFontStyle;
        public FontStyle MonoFontStyle;

        public readonly Element RootElement;
        public Element FocusedElement { get; private set; }
        public Element HoveredElement { get; private set; }
        public bool IsHoveredElementPressed { get; private set; }

        public int MouseX { get; private set; }
        public int MouseY { get; private set; }

        // Animations
        readonly List<Action<float>> _animationActions = new List<Action<float>>();
        readonly List<Action<float>> _newAnimationActions = new List<Action<float>>();
        readonly List<Action<float>> _removedAnimationActions = new List<Action<float>>();

        // Input
        bool _leftShiftDown = false;
        bool _rightShiftDown = false;

        bool _leftCtrlDown = false;
        bool _rightCtrlDown = false;

        // Drawing
        readonly Stack<SDL.SDL_Rect> _clipStack = new Stack<SDL.SDL_Rect>();

        public Desktop(IntPtr renderer, FontStyle mainFontStyle, FontStyle monoFontStyle)
        {
            Renderer = renderer;
            MainFontStyle = mainFontStyle;
            MonoFontStyle = monoFontStyle;

            RootElement = new Element(this, null);
            RootElement.Mount();
        }

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

        public void MoveFocus(bool backwards)
        {
            var element = FocusedElement;

            while (element != null)
            {
                var nextOrPreviousElement = !backwards ? GetNextSibling(element) : GetPreviousSibling(element);
                if (nextOrPreviousElement == null) element = element.Parent;
                else { element = nextOrPreviousElement; break; }
            }

            if (element == null) element = RootElement;
            element = !backwards ? GetFirstFocusable(element) : GetLastFocusable(element);
            if (element == null && backwards) element = GetLastFocusable(RootElement);
            if (element != null) SetFocusedElement(element);

            Element GetNextSibling(Element element)
            {
                if (element.Parent == null) return null;

                var siblings = element.Parent.Children;
                var index = siblings.IndexOf(element) + 1;

                while (index < siblings.Count)
                {
                    if (siblings[index].IsMounted) return siblings[index];
                    index++;
                }

                return null;
            }

            Element GetPreviousSibling(Element element)
            {
                if (element.Parent == null) return null;

                var siblings = element.Parent.Children;
                var index = siblings.IndexOf(element) - 1;

                while (index >= 0)
                {
                    if (siblings[index].IsMounted) return siblings[index];
                    index--;
                }

                return null;
            }

            Element GetFirstFocusable(Element element)
            {
                if (element.AcceptsFocus()) return element;

                if (element.Children.Count > 0)
                {
                    foreach (var child in element.Children)
                    {
                        if (!child.IsMounted) continue;
                        var focusable = GetFirstFocusable(child);
                        if (focusable != null) return focusable;
                    }
                }

                var nextInTree = GetNextSibling(element) ?? GetNextSibling(element.Parent);
                return nextInTree != null ? GetFirstFocusable(nextInTree) : null;
            }

            Element GetLastFocusable(Element element)
            {
                if (element.AcceptsFocus()) return element;

                if (element.Children.Count > 0)
                {
                    for (var i = element.Children.Count - 1; i >= 0; i--)
                    {
                        var child = element.Children[i];
                        if (!child.IsMounted) continue;
                        var focusable = GetLastFocusable(child);
                        if (focusable != null) return focusable;
                    }
                }

                var previousInTree = GetPreviousSibling(element) ?? GetPreviousSibling(element.Parent);
                return previousInTree != null ? GetLastFocusable(previousInTree) : null;
            }
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

        public void Draw()
        {
            RootElement.Draw();

            if (FocusedElement != null && FocusedElement.OutlineColor.A != 0)
            {

                var clipAncestor = FocusedElement;
                while (clipAncestor != null && clipAncestor.HorizontalFlow != Flow.Scroll && clipAncestor.VerticalFlow != Flow.Scroll) clipAncestor = clipAncestor.Parent;
                if (clipAncestor != null) PushClipRect(clipAncestor.ViewRectangle);

                SDL.SDL_SetRenderDrawBlendMode(Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
                FocusedElement.OutlineColor.UseAsDrawColor(Renderer);
                FocusedElement.DrawOutline();
                SDL.SDL_SetRenderDrawBlendMode(Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);

                if (clipAncestor != null) PopClipRect();
            }

            Debug.Assert(_clipStack.Count == 0);

#if DEBUG && false
            SDL.SDL_SetRenderDrawBlendMode(Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
            new Color(0xff00ff44).UseAsDrawColor(Renderer);

            void DrawOutline(Element element)
            {
                if (HoveredElement == element) new Color(0xff0000ff).UseAsDrawColor(Renderer);
                element.DrawOutline();
                if (HoveredElement == element) new Color(0xff00ff44).UseAsDrawColor(Renderer);

                foreach (var child in element.Children) if (child.IsMounted) DrawOutline(child);
            }

            DrawOutline(RootElement);
            SDL.SDL_SetRenderDrawBlendMode(Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);

            Color.White.UseAsDrawColor(Renderer);
            if (HoveredElement != null) MainFontStyle.DrawText(5, 5, $"{HoveredElement.GetType().Name} {HoveredElement.LayoutRectangle.X} {HoveredElement.LayoutRectangle.Y}");
#endif
        }

        internal void PushClipRect(Rectangle clipRect)
        {
            var sdlRect = clipRect.ToSDL_Rect();
            SDL.SDL_RenderSetClipRect(Renderer, ref sdlRect);
            _clipStack.Push(sdlRect);
        }

        internal void PopClipRect()
        {
            _clipStack.Pop();

            if (_clipStack.Count > 0)
            {
                var sdlRect = _clipStack.Peek();
                SDL.SDL_RenderSetClipRect(Renderer, ref sdlRect);
            }
            else
            {
                SDL.SDL_RenderSetClipRect(Renderer, IntPtr.Zero);
            }
        }
        #endregion
    }
}
