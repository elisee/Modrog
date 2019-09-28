using DeepSwarmClient.UI;
using DeepSwarmCommon;

namespace DeepSwarmClient.Interface
{
    class EnterNameView : InterfaceElement
    {
        readonly TextInput _nameInput;

        public EnterNameView(Interface @interface)
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
                Text = "Enter your name:"
            };

            _nameInput = new TextInput(Desktop, panel)
            {
                Anchor = new Anchor(left: 8, top: 48, width: 320, height: 16),
                BackgroundColor = new Color(0x004400ff),
                MaxLength = Protocol.MaxPlayerNameLength
            };

            new Button(Desktop, panel)
            {
                Text = " OK ",
                Anchor = new Anchor(left: 8, top: 80, width: " OK ".Length * 16, height: 16),
                BackgroundColor = new Color(0x4444ccff),
                OnActivate = Validate
            };
        }

        public override void OnMounted()
        {
            _nameInput.SetValue(Engine.State.SelfPlayerName ?? "");
            Desktop.SetFocusedElement(_nameInput);
        }

        public override void Validate()
        {
            var name = _nameInput.Value.Trim();
            if (name.Length == 0) return;

            Engine.State.SetName(name);
        }
    }
}
