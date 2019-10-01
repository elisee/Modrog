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
                Anchor = new Anchor { Width = 320, Flow = Flow.Shrink },
                Padding = new Padding { All = 8 },
                ChildLayout = ChildLayoutMode.Top
            };

            new Label(Desktop, panel) { Text = "- DeepSwarm r1 -", Anchor = new Anchor { Flow = Flow.Shrink, Bottom = 16 } };
            new Label(Desktop, panel) { Text = "Enter your name:", Anchor = new Anchor { Bottom = 8 } };

            _nameInput = new TextInput(Desktop, panel)
            {
                Padding = new Padding { All = 8 },
                Anchor = new Anchor { Height = 32, Bottom = 8 },
                BackgroundPatch = new TexturePatch(0x004400ff),
                MaxLength = Protocol.MaxPlayerNameLength
            };

            new Label(Desktop, panel) { Text = "Server address:", Anchor = new Anchor { Bottom = 8 } };

            _serverAddressInput = new TextInput(Desktop, panel)
            {
                Padding = new Padding { All = 8 },
                Anchor = new Anchor { Height = 32 },
                BackgroundPatch = new TexturePatch(0x004400ff)
            };

            var actionsContainer = new Element(Desktop, panel) { ChildLayout = ChildLayoutMode.Left, Anchor = new Anchor { Top = 16 } };

            new TextButton(Desktop, actionsContainer)
            {
                Text = "Connect",
                Padding = new Padding { All = 8 },
                Anchor = new Anchor { Flow = Flow.Shrink },
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
