using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.UI;

namespace DeepSwarmScenarioEditor.Interface.Editing.Manifest
{
    class ManifestEditor : InterfaceElement
    {
        readonly Label _nameLabel;
        readonly TextInput _titleInput;
        readonly TextEditor _descriptionEditor;

        public ManifestEditor(Interface @interface, Element parent)
            : base(@interface, null)
        {
            ChildLayout = ChildLayoutMode.Top;
            Padding = 8;

            new Label(this) { Text = "Name", Bottom = 8 };

            _nameLabel = new Label(this)
            {
                Bottom = 8,
                Padding = 8,
                BackgroundPatch = new TexturePatch(0x004400ff),
            };

            new Label(this) { Text = "Title", Bottom = 8 };

            _titleInput = new TextInput(this)
            {
                Bottom = 8,
                Padding = 8,
                BackgroundPatch = new TexturePatch(0x004400ff),
            };

            new Label(this) { Text = "Description", Bottom = 8 };

            _descriptionEditor = new TextEditor(this)
            {
                Height = 200,
                Padding = 8,
                BackgroundPatch = new TexturePatch(0x004400ff),
            };

            parent?.Add(this);
        }

        public override void OnMounted()
        {
            var scenarioEntry = Engine.State.ActiveScenarioEntry;

            _nameLabel.Text = scenarioEntry.Name;
            _titleInput.SetValue(scenarioEntry.Title);
            _descriptionEditor.SetText(scenarioEntry.Description);
        }
    }
}
