using DeepSwarmClient.UI;
using DeepSwarmCommon;

namespace DeepSwarmClient.Interface.Playing
{
    class RenameScriptPopup : InterfaceElement
    {
        readonly TextInput _scriptPathInput;

        public RenameScriptPopup(Interface @interface)
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
                Text = "- Rename script -"
            };

            new Label(Desktop, panel)
            {
                Anchor = new Anchor(left: 8, top: 24),
                Text = "Script name:"
            };

            _scriptPathInput = new TextInput(Desktop, panel)
            {
                Anchor = new Anchor(left: 8, top: 48, width: 320, height: 16),
                BackgroundColor = new Color(0x004400ff),
                MaxLength = Protocol.MaxScriptNameLength
            };

            var renameButton = new Button(Desktop, panel)
            {
                Text = " Rename ",
                Anchor = new Anchor(left: 8, top: 80, width: " Rename ".Length * 16, height: 16),
                BackgroundColor = new Color(0x4444ccff),
                OnActivate = Validate
            };

            new Button(Desktop, panel)
            {
                Text = " Cancel ",
                Anchor = new Anchor(left: 8 + renameButton.Anchor.Width + 8, top: 80, width: " Cancel ".Length * 16, height: 16),
                BackgroundColor = new Color(0x4444ccff),
                OnActivate = Dismiss
            };
        }

        public override void OnMounted()
        {
            _scriptPathInput.SetValue(Engine.State.EntityScriptPaths[Engine.State.SelectedEntity.Id]);
            Desktop.SetFocusedElement(_scriptPathInput);
        }

        public override void Validate()
        {
            var relativePath = _scriptPathInput.Value.Trim();

            // TODO: Display errors
            if (relativePath.Length == 0 || relativePath.Contains("..")) return;
            if (Engine.State.Scripts.ContainsKey(relativePath)) return;

            Engine.State.RenameSelectedEntityScript(relativePath);
            Engine.Interface.SetPopup(null);
            Desktop.SetFocusedElement(Engine.Interface.PlayingView);
        }

        public override void Dismiss()
        {
            Engine.Interface.SetPopup(null);
            Desktop.SetFocusedElement(Engine.Interface.PlayingView);
        }
    }
}
