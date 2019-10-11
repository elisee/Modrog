using DeepSwarmClient.Graphics;
using DeepSwarmClient.UI;

namespace DeepSwarmClient.Interface.Playing
{
    class PlayingMenu : InterfaceElement
    {
        public PlayingMenu(Interface @interface, PlayingView view) : base(@interface, view)
        {
            BackgroundPatch = new TexturePatch(0x00000088);

            var panel = new Panel(this)
            {
                Flow = Flow.Shrink,
                ChildLayout = ChildLayoutMode.Top,
                BackgroundPatch = new TexturePatch(0x123456ff),
                Padding = 16
            };

            new TextButton(panel)
            {
                Text = "Back to game",
                OnActivate = () => Engine.State.SetPlayingMenuOpen(false),
                Padding = 8,
                Bottom = 8,
            }.Label.Flow = Flow.Shrink;

            new TextButton(panel)
            {
                Text = "Quit",
                OnActivate = () => Engine.State.Disconnect(),
                Padding = 8,
            }.Label.Flow = Flow.Shrink;
        }

        public override void OnMounted()
        {
            Desktop.SetFocusedElement(this);
        }

        public override void Dismiss()
        {
            Engine.State.SetPlayingMenuOpen(false);
        }
    }
}
