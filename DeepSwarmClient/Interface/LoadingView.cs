using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.UI;

namespace DeepSwarmClient.Interface
{
    class LoadingView : InterfaceElement
    {
        readonly Label _loadingLabel;

        public LoadingView(Interface @interface)
            : base(@interface, null)
        {
            var loadingPopup = new Element(Desktop, this)
            {
                Flow = Flow.Shrink,
                Padding = 16,
                BackgroundPatch = new TexturePatch(0x88aa88ff)
            };

            _loadingLabel = new Label(loadingPopup);
        }

        public override void OnMounted()
        {
            OnProgress();
        }

        public void OnProgress()
        {
            _loadingLabel.Text = Engine.State.LoadingProgressText;
            _loadingLabel.Parent.Layout();
        }
    }
}
