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
            var panel = new Panel(Desktop, this, new TexturePatch(0x88aa88ff))
            {
                Width = 320,
                Flow = Flow.Shrink,
                Padding = 8,
                ChildLayout = ChildLayoutMode.Top
            };

            new Label(Desktop, panel) { Text = "- DeepSwarm r1 -", Flow = Flow.Shrink, Bottom = 16 };
            new Label(Desktop, panel) { Text = "Enter your name:", Bottom = 8 };

            _nameInput = new TextInput(Desktop, panel)
            {
                Height = 32,
                Bottom = 8,
                Padding = 8,
                BackgroundPatch = new TexturePatch(0x004400ff),
                MaxLength = Protocol.MaxPlayerNameLength
            };

            new Label(Desktop, panel) { Text = "Server address:", Bottom = 8 };

            _serverAddressInput = new TextInput(Desktop, panel)
            {
                Height = 32,
                Padding = 8,
                BackgroundPatch = new TexturePatch(0x004400ff)
            };

            var actionsContainer = new Element(Desktop, panel) { ChildLayout = ChildLayoutMode.Left, Top = 16 };

            new TextButton(Desktop, actionsContainer)
            {
                Text = "Connect",
                Padding = 8,
                Flow = Flow.Shrink,
                BackgroundPatch = new TexturePatch(0x4444aaff),
                OnActivate = Validate
            };
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
