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

            var panel = new Element(Desktop, this)
            {
                AnchorRectangle = new Rectangle(16, 16, 320 + 32, 320 + 32),
                BackgroundColor = new Color(0x88aa88ff)
            };

            new Label(Desktop, panel) { Text = "Enter your name:" };

            NameInput = new TextInput(Desktop, panel)
            {
                AnchorRectangle = new Rectangle(16, 32, 320, 16),
                BackgroundColor = new Color(0x004400ff),
                MaxLength = Protocol.MaxPlayerNameLength
            };

            new Button(Desktop, panel)
            {
                Text = "OK",
                AnchorRectangle = new Rectangle(16, 64, ("OK".Length + 2) * 16, 16),
                BackgroundColor = new Color(0x4444ccff),
                OnActivate = OnValidate
            };
        }

        public void OnValidate()
        {
            var name = NameInput.Value.Trim();
            if (name.Length == 0) return;

            Engine.SetName(name);
        }
    }
}
