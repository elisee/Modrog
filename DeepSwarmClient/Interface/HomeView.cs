using DeepSwarmCommon;
using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.Interface;
using DeepSwarmPlatform.UI;
using System.Globalization;

namespace DeepSwarmClient.Interface
{
    class HomeView : InterfaceElement
    {
        readonly TextInput _nameInput;
        readonly TextInput _serverHostnameInput;
        readonly TextInput _serverPortInput;

        readonly Label _errorLabel;

        public HomeView(Interface @interface)
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

            {
                var serverAddressContainer = new Panel(mainPanel) { ChildLayout = ChildLayoutMode.Left, Bottom = 8 };

                new Label(serverAddressContainer) { Text = "Hostname:", VerticalFlow = Flow.Shrink, RightPadding = 8 };

                _serverHostnameInput = new TextInput(serverAddressContainer)
                {
                    Padding = 8,
                    LayoutWeight = 1,
                    BackgroundPatch = new TexturePatch(0x004400ff)
                };

                new Label(serverAddressContainer) { Text = "Port:", VerticalFlow = Flow.Shrink, HorizontalPadding = 8 };

                _serverPortInput = new TextInput(serverAddressContainer)
                {
                    Padding = 8,
                    Width = 100,
                    BackgroundPatch = new TexturePatch(0x004400ff)
                };
            }

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
                _errorLabel.Text = Engine.State.ErrorMessage + (Engine.State.KickReason != null ? $" Reason: {Engine.State.KickReason}" : "");
            }
            else
            {
                _errorLabel.IsVisible = false;
            }

            _nameInput.SetValue(Engine.State.SelfPlayerName ?? "");
            _serverHostnameInput.SetValue(Engine.State.SavedServerHostname ?? "localhost");
            _serverPortInput.SetValue(Engine.State.SavedServerPort.ToString(CultureInfo.InvariantCulture));

            Desktop.SetFocusedElement(_nameInput);
        }

        public override void Validate()
        {
            var name = _nameInput.Value.Trim();
            if (name.Length == 0) return;

            var hostname = _serverHostnameInput.Value.Trim();
            if (hostname.Length == 0)
            {
                // TODO: Set error state
                Desktop.SetFocusedElement(_serverHostnameInput);
                _serverHostnameInput.SelectAll();
                return;
            }

            if (_serverPortInput.Value.Trim() == string.Empty)
            {
                _serverPortInput.SetValue(Protocol.Port.ToString(CultureInfo.InvariantCulture));
            }

            if (!int.TryParse(_serverPortInput.Value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port > ushort.MaxValue)
            {
                // TODO: Set error state
                Desktop.SetFocusedElement(_serverPortInput);
                _serverPortInput.SelectAll();
                return;
            }

            Engine.State.SetName(name);
            Engine.State.Connect(hostname, port);
        }
    }
}
