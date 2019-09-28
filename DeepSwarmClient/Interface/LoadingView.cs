using DeepSwarmClient.UI;

namespace DeepSwarmClient.Interface
{
    class LoadingView : InterfaceElement
    {
        public LoadingView(Interface @interface)
            : base(@interface, null)
        {
            var loadingPopup = new Element(Desktop, this)
            {
                Anchor = new Anchor(width: 320, height: 320),
                BackgroundColor = new Color(0x88aa88ff)
            };

            new Label(Desktop, loadingPopup) { Text = "Loading" };
        }
    }
}
