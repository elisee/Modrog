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

            var nameRow = new Panel(mainPanel) { ChildLayout = ChildLayoutMode.Left, Bottom = 8 };
            new Label(nameRow) { Text = "Your name:", VerticalFlow = Flow.Shrink, Right = 8 };

            _nameInput = new TextInput(nameRow)
            {
                Padding = 8,
                BackgroundPatch = new TexturePatch(0x004400ff),
                MaxLength = Protocol.MaxPlayerNameLength,
                LayoutWeight = 1,
            };

            {
                var startServerPanel = new Panel(mainPanel)
                {
                    BackgroundPatch = new TexturePatch(0x00000044),
                    ChildLayout = ChildLayoutMode.Top,
                    Padding = 8,
                    Bottom = 8,
                    OnValidate = OnSubmitStartServer
                };

                new Label(startServerPanel) { Text = "PLAY SOLO OR INVITE FRIENDS", Bottom = 8 };

                var actionsContainer = new Element(startServerPanel) { ChildLayout = ChildLayoutMode.Left };

                new StyledTextButton(actionsContainer)
                {
                    Text = "Start server",
                    OnActivate = OnSubmitStartServer
                };
            }

            {
                var connectToServerPanel = new Panel(mainPanel)
                {
                    BackgroundPatch = new TexturePatch(0x00000044),
                    ChildLayout = ChildLayoutMode.Top,
                    Padding = 8,
                    OnValidate = OnSubmitConnectToServer
                };

                new Label(connectToServerPanel) { Text = "JOIN FRIENDS", Bottom = 8 };

                var hostnameRow = new Panel(connectToServerPanel) { ChildLayout = ChildLayoutMode.Left, Bottom = 8 };
                new Label(hostnameRow) { Text = "Hostname / IP:", VerticalFlow = Flow.Shrink, Width = 140, Right = 8 };

                _serverHostnameInput = new TextInput(hostnameRow)
                {
                    Padding = 8,
                    LayoutWeight = 1,
                    BackgroundPatch = new TexturePatch(0x004400ff)
                };

                var portRow = new Panel(connectToServerPanel) { ChildLayout = ChildLayoutMode.Left, Bottom = 8 };
                new Label(portRow) { Text = "Port:", VerticalFlow = Flow.Shrink, Width = 140, Right = 8 };

                _serverPortInput = new TextInput(portRow)
                {
                    Padding = 8,
                    LayoutWeight = 1,
                    BackgroundPatch = new TexturePatch(0x004400ff)
                };

                var actionsContainer = new Element(connectToServerPanel) { ChildLayout = ChildLayoutMode.Left };

                new StyledTextButton(actionsContainer)
                {
                    Text = "Connect",
                    OnActivate = OnSubmitConnectToServer
                };
            }

            _errorLabel = new Label(mainPanel)
            {
                BackgroundPatch = new TexturePatch(0xff4444ff),
                Top = 8,
                Padding = 8,
                Wrap = true,
                Visible = false
            };
        }

        public override void OnMounted()
        {
            if (Engine.State.ErrorMessage != null)
            {
                _errorLabel.Visible = true;
                _errorLabel.Text = Engine.State.ErrorMessage + (Engine.State.KickReason != null ? $" Reason: {Engine.State.KickReason}" : "");
            }
            else
            {
                _errorLabel.Visible = false;
            }

            _nameInput.SetValue(Engine.State.SelfPlayerName ?? "");
            _serverHostnameInput.SetValue(Engine.State.SavedServerHostname ?? "localhost");
            _serverPortInput.SetValue(Engine.State.SavedServerPort.ToString(CultureInfo.InvariantCulture));

            Desktop.SetFocusedElement(_nameInput);
        }

        void OnSubmitConnectToServer()
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

        void OnSubmitStartServer()
        {
            Engine.State.StartServer(scenario: null);
        }
    }
}
