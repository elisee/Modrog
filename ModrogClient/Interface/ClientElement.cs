using SwarmPlatform.UI;

namespace ModrogClient.Interface
{
    class ClientElement : Element
    {
        public readonly ClientApp App;

        public ClientElement(ClientApp app, Element parent)
            : base(app.Desktop, null)
        {
            App = app;
            parent?.Add(this);
        }

    }
}
