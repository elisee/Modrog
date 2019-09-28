using DeepSwarmClient.UI;

namespace DeepSwarmClient.Interface
{
    class ConnectView : InterfaceElement
    {
        readonly TextInput _serverAddressInput;

        public ConnectView(Interface @interface)
            : base(@interface, null)
        {
            const int PanelWidth = 320 + 16 * 2;
            const int PanelHeight = 160 + 16 * 2;

            var panel = new Panel(Desktop, this, new Color(0x88aa88ff))
            {
                Anchor = new Anchor(width: PanelWidth, height: PanelHeight),
            };

            new Label(Desktop, panel)
            {
                Anchor = new Anchor(left: 8, top: 8),
                Text = "- DeepSwarm r1 -"
            };

            new Label(Desktop, panel)
            {
                Anchor = new Anchor(left: 8, top: 24),
                Text = "Server address:"
            };

            _serverAddressInput = new TextInput(Desktop, panel)
            {
                Anchor = new Anchor(left: 8, top: 48, width: 320, height: 16),
                BackgroundColor = new Color(0x004400ff)
            };

            new Button(Desktop, panel)
            {
                Text = " Connect ",
                Anchor = new Anchor(left: 8, top: 80, width: " Connect ".Length * 16, height: 16),
                BackgroundColor = new Color(0x4444ccff),
                OnActivate = Validate
            };
        }


        public override void OnMounted()
        {
            _serverAddressInput.SetValue(Engine.State.SavedServerAddress ?? "localhost");
            Desktop.SetFocusedElement(_serverAddressInput);
        }

        public override void Validate()
        {
            var address = _serverAddressInput.Value.Trim();
            if (address.Length == 0) return;

            Engine.State.Connect(address);
        }
    }
}
