using SwarmPlatform.Graphics;
using SwarmPlatform.UI;
using System;
using System.IO;
using System.Text.Json;

namespace ModrogEditor.Interface.Editing.Manifest
{
    class ManifestEditor : BaseEditor
    {
        readonly Label _nameLabel;
        readonly TextInput _titleInput;
        readonly TextEditor _descriptionEditor;

        public ManifestEditor(EditorApp @interface, string fullAssetPath, Action onUnsavedStatusChanged)
            : base(@interface, fullAssetPath, onUnsavedStatusChanged)
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
                OnChange = MarkUnsavedChanges
            };

            new Label(this) { Text = "Description", Bottom = 8 };

            _descriptionEditor = new TextEditor(this)
            {
                Height = 200,
                Padding = 8,
                BackgroundPatch = new TexturePatch(0x004400ff),
                OnChange = MarkUnsavedChanges
            };

            Load();
        }

        public override void OnMounted()
        {
            Desktop.SetFocusedElement(_titleInput);
        }

        protected override bool TryLoad(out string error)
        {
            var scenarioEntry = App.State.ActiveScenarioEntry;

            _nameLabel.Text = scenarioEntry.Name;
            _titleInput.SetValue(scenarioEntry.Title);
            _descriptionEditor.SetText(scenarioEntry.Description);

            error = null;
            return true;
        }

        protected override void Unload()
        {
            // Nothing
        }

        protected override bool TrySave_Internal(out string error)
        {
            var title = App.State.ActiveScenarioEntry.Title = _titleInput.Value.Trim();
            var description = App.State.ActiveScenarioEntry.Description = _descriptionEditor.GetText().Trim();

            try
            {
                using var stream = File.OpenWrite(FullAssetPath);
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

                writer.WriteStartObject();

                writer.WriteString("title", title);
                writer.WriteString("description", description);

                writer.WritePropertyName("minMaxPlayers");
                writer.WriteStartArray();
                writer.WriteNumberValue(1);
                writer.WriteNumberValue(1);
                writer.WriteEndArray();

                writer.WriteEndObject();
            }
            catch (Exception exception)
            {
                error = "Error while saving manifest: " + exception.Message;
                return false;
            }

            error = null;
            return true;
        }
    }
}
