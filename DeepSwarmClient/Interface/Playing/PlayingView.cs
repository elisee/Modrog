using DeepSwarmClient.UI;
using SDL2;

namespace DeepSwarmClient.Interface.Playing
{
    class PlayingView : InterfaceElement
    {
        public PlayingView(Interface @interface)
            : base(@interface, null)
        {
        }

        public override Element HitTest(int x, int y)
        {
            return base.HitTest(x, y) ?? (LayoutRectangle.Contains(x, y) ? this : null);
        }

        public override void OnMounted()
        {
            Desktop.RegisterAnimation(Animate);
            Desktop.SetFocusedElement(this);
        }

        public override void OnUnmounted()
        {
            Desktop.UnregisterAnimation(Animate);
        }

        public override void OnKeyDown(SDL.SDL_Keycode key, bool repeat)
        {
            if (repeat) return;
        }

        public override void OnKeyUp(SDL.SDL_Keycode key)
        {
        }

        public override void OnMouseMove()
        {
        }

        public override void OnMouseDown(int button)
        {
        }

        public override void OnMouseUp(int button)
        {
        }

        public override void OnMouseWheel(int dx, int dy)
        {
        }

        public void OnPlayerListUpdated()
        {
        }

        public void OnChatMessageReceived(string author, string message)
        {
        }

        public void Animate(float deltaTime)
        {
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();
        }
    }
}
