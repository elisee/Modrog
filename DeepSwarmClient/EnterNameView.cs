using DeepSwarmClient.UI;
using DeepSwarmCommon;

namespace DeepSwarmClient
{
    class EnterNameView : EngineElement
    {
        public readonly TextInput NameInput;

        public EnterNameView(Engine engine)
            : base(engine, null)
        {
            AnchorRectangle = engine.Viewport;

            const int PanelWidth = 320 + 16 * 2;
            const int PanelHeight = 160 + 16 * 2;

            var panel = new Panel(Desktop, this, new Color(0x88aa88ff))
            {
                AnchorRectangle = MakeCenteredRectangle(PanelWidth, PanelHeight),
            };

            new Label(Desktop, panel)
            {
                AnchorRectangle = new Rectangle(8, 8, 0, 0),
                Text = "- DeepSwarm r1 -"
            };

            new Label(Desktop, panel)
            {
                AnchorRectangle = new Rectangle(8, 24, 0, 0),
                Text = "Enter your name:"
            };

            NameInput = new TextInput(Desktop, panel)
            {
                AnchorRectangle = new Rectangle(8, 48, 320, 16),
                BackgroundColor = new Color(0x004400ff),
                MaxLength = Protocol.MaxPlayerNameLength
            };

            new Button(Desktop, panel)
            {
                Text = "OK",
                AnchorRectangle = new Rectangle(8, 80, ("OK".Length + 2) * 16, 16),
                BackgroundColor = new Color(0x4444ccff),
                OnActivate = Validate
            };
        }

        public override void Validate()
        {
            var name = NameInput.Value.Trim();
            if (name.Length == 0) return;

            Engine.FromUI_SetName(name);
        }
    }
}
