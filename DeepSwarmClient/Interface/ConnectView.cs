using DeepSwarmClient.UI;
using DeepSwarmCommon;

namespace DeepSwarmClient.Interface
{
    class ConnectView : InterfaceElement
    {
        readonly TextInput _nameInput;
        readonly TextInput _serverAddressInput;

        readonly Label _errorLabel;

        public ConnectView(Interface @interface)
            : base(@interface, null)
        {
            var windowPanel = new Panel(this, new TexturePatch(0x228800ff))
            {
                Width = 480,
                Flow = Flow.Shrink,
                ChildLayout = ChildLayoutMode.Top,
            };

            var titlePanel = new Panel(windowPanel, new TexturePatch(0x88aa88ff));
            new Label(titlePanel) { Text = "- DeepSwarm r2 -", Flow = Flow.Shrink, Padding = 8 };

            var mainPanel = new Panel(windowPanel, new TexturePatch(0x228800ff))
            {
                Padding = 8,
                ChildLayout = ChildLayoutMode.Top,
            };

            new Label(mainPanel) { Text = "Enter your name:", Bottom = 8 };

            _nameInput = new TextInput(mainPanel)
            {
                Bottom = 8,
                Padding = 8,
                BackgroundPatch = new TexturePatch(0x004400ff),
                MaxLength = Protocol.MaxPlayerNameLength
            };

            new Label(mainPanel) { Text = "Server address:", Bottom = 8 };

            _serverAddressInput = new TextInput(mainPanel)
            {
                Padding = 8,
                BackgroundPatch = new TexturePatch(0x004400ff)
            };

            _errorLabel = new Label(mainPanel)
            {
                BackgroundPatch = new TexturePatch(0xff4444ff),
                Top = 8,
                Padding = 8,
                Wrap = true,
                IsVisible = false
            };

            var actionsContainer = new Element(mainPanel) { ChildLayout = ChildLayoutMode.Left, Top = 16 };

            new StyledTextButton(actionsContainer)
            {
                Text = "Connect",
                OnActivate = Validate
            };
        }

        public override void OnMounted()
        {
            if (Engine.State.ErrorMessage != null)
            {
                _errorLabel.IsVisible = true;
                _errorLabel.Text = Engine.State.ErrorMessage;
            }
            else
            {
                _errorLabel.IsVisible = false;
            }

            _nameInput.SetValue(Engine.State.SelfPlayerName ?? "");
            _serverAddressInput.SetValue(Engine.State.SavedServerAddress ?? "localhost");

            Desktop.SetFocusedElement(_nameInput);
        }

        public override void Validate()
        {
            var name = _nameInput.Value.Trim();
            if (name.Length == 0) return;

            var address = _serverAddressInput.Value.Trim();
            if (address.Length == 0) return;

            Engine.State.SetName(name);
            Engine.State.Connect(address);
        }
    }
}
