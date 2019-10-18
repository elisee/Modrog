using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.Interface;
using DeepSwarmPlatform.UI;

namespace DeepSwarmClient.Interface
{
    class LoadingView : InterfaceElement
    {
        readonly Label _loadingLabel;
        readonly StyledTextButton _abortButton;

        public LoadingView(Interface @interface)
            : base(@interface, null)
        {
            var loadingPopup = new Element(Desktop, this)
            {
                Flow = Flow.Shrink,
                Width = 320,
                Padding = 8,
                BackgroundPatch = new TexturePatch(0x88aa88ff),
                ChildLayout = ChildLayoutMode.Top,
            };

            _loadingLabel = new Label(loadingPopup) { Wrap = true, Bottom = 24 };

            _abortButton = new StyledTextButton(loadingPopup)
            {
                Text = "Abort",
                OnActivate = () => Engine.State.Disconnect()
            };
        }

        public override void OnMounted()
        {
            OnProgress();

            Desktop.SetFocusedElement(_abortButton);
        }

        public void OnProgress()
        {
            _loadingLabel.Text = Engine.State.LoadingProgressText;
            _loadingLabel.Parent.Layout();
        }
    }
}
