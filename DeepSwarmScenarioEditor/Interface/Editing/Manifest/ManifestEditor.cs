﻿using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.UI;
using System.IO;
using System.Text.Json;

namespace DeepSwarmScenarioEditor.Interface.Editing.Manifest
{
    class ManifestEditor : BaseAssetEditor
    {
        readonly Label _nameLabel;
        readonly TextInput _titleInput;
        readonly TextEditor _descriptionEditor;

        public ManifestEditor(Interface @interface, string fullAssetPath)
            : base(@interface, fullAssetPath)
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
        }

        public override void OnMounted()
        {
            var scenarioEntry = Engine.State.ActiveScenarioEntry;

            _nameLabel.Text = scenarioEntry.Name;
            _titleInput.SetValue(scenarioEntry.Title);
            _descriptionEditor.SetText(scenarioEntry.Description);

            Desktop.SetFocusedElement(_titleInput);
        }

        public override void OnUnmounted()
        {
            Save();
        }

        void Save()
        {
            var title = Engine.State.ActiveScenarioEntry.Title = _titleInput.Value.Trim();
            var description = Engine.State.ActiveScenarioEntry.Description = _descriptionEditor.GetText().Trim();

            using (var stream = File.OpenWrite(FullAssetPath))
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                writer.WriteString("title", title);
                writer.WriteString("description", description);

                writer.WritePropertyName("minMaxPlayers");
                writer.WriteNumberValue(1);
                writer.WriteNumberValue(1);
                writer.WriteEndArray();

                writer.WriteEndObject();
            }
        }
    }
}
