﻿using SDL2;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DeepSwarmClient.UI
{
    class Desktop
    {
        public IntPtr Renderer;

        public Element RootElement { get; private set; }
        public Element FocusedElement;

        public Desktop(IntPtr renderer)
        {
            Renderer = renderer;
        }

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

        public void SetRootElement(Element element)
        {
            RootElement = element;
            RootElement.Layout(new Rectangle(0, 0, 1280, 720));
        }

        public void Draw() => RootElement.Draw();
    }
}
