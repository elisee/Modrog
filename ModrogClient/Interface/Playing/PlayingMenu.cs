using SwarmPlatform.Graphics;
using SwarmPlatform.UI;

namespace ModrogClient.Interface.Playing
{
    class PlayingMenu : ClientElement
    {
        public PlayingMenu(ClientApp app, PlayingView view) : base(app, view)
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
                OnActivate = () => App.State.SetPlayingMenuOpen(false),
                Padding = 8,
                Bottom = 8,
            }.Label.Flow = Flow.Shrink;

            new TextButton(panel)
            {
                Text = "Quit",
                OnActivate = () => App.State.Disconnect(),
                Padding = 8,
            }.Label.Flow = Flow.Shrink;
        }

        public override void OnMounted()
        {
            Desktop.SetFocusedElement(this);
        }

        public override void Dismiss()
        {
            App.State.SetPlayingMenuOpen(false);
        }
    }
}
