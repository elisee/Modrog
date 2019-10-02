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
                Width = 320,
                Height = 320,
                BackgroundPatch = new TexturePatch(0x88aa88ff)
            };

            new Label(loadingPopup) { Text = "Loading" };
        }
    }
}
