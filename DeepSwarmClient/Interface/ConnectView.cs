using DeepSwarmClient.UI;
using DeepSwarmCommon;

namespace DeepSwarmClient.Interface
{
    class ConnectView : InterfaceElement
    {
        readonly TextInput _nameInput;
        readonly TextInput _serverAddressInput;

        public ConnectView(Interface @interface)
            : base(@interface, null)
        {
            const int PanelWidth = 320 + 16 * 2;

            var panel = new Panel(Desktop, this, new Color(0x88aa88ff))
            {
                Anchor = new Anchor(width: PanelWidth),
            };

            var y = 8;

            new Label(Desktop, panel)
            {
                Anchor = new Anchor(left: 8, top: y),
                Text = "- DeepSwarm r1 -"
            };

            y += 16 + 16;

            new Label(Desktop, panel)
            {
                Anchor = new Anchor(left: 8, top: y),
                Text = "Enter your name:"
            };

            y += 16 + 8;

            _nameInput = new TextInput(Desktop, panel)
            {
                Anchor = new Anchor(left: 8, top: y, width: 320, height: 16),
                BackgroundColor = new Color(0x004400ff),
                MaxLength = Protocol.MaxPlayerNameLength
            };

            y += 16 + 16;

            new Label(Desktop, panel)
            {
                Anchor = new Anchor(left: 8, top: y),
                Text = "Server address:"
            };

            y += 16 + 8;

            _serverAddressInput = new TextInput(Desktop, panel)
            {
                Anchor = new Anchor(left: 8, top: y, width: 320, height: 16),
                BackgroundColor = new Color(0x004400ff)
            };

            y += 16 + 16;

            new Button(Desktop, panel)
            {
                Text = " Connect ",
                Anchor = new Anchor(left: 8, top: y, width: " Connect ".Length * 16, height: 16),
                BackgroundColor = new Color(0x4444ccff),
                OnActivate = Validate
            };

            y += 16 + 8;

            panel.Anchor.Height = y;
        }

        public override void OnMounted()
        {
            _nameInput.SetValue(Engine.State.SelfPlayerName ?? "");
            _serverAddressInput.SetValue(Engine.State.SavedServerAddress ?? "localhost");

            Desktop.SetFocusedElement(_nameInput);
        }

        public override void Validate()
        {
            // TODO: Display errors
            var name = _nameInput.Value.Trim();
            if (name.Length == 0) return;

            var address = _serverAddressInput.Value.Trim();
            if (address.Length == 0) return;

            Engine.State.SetName(name);
            Engine.State.Connect(address);
        }
    }
}
