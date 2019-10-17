using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.Interface;
using DeepSwarmPlatform.UI;
using System.IO;

namespace DeepSwarmScenarioEditor.Interface.Editing.Script
{
    class ScriptEditor : BaseAssetEditor
    {
        TextEditor _textEditor;

        public ScriptEditor(Interface @interface, string fullAssetPath)
            : base(@interface, fullAssetPath)
        {
            var mainLayer = new Panel(this)
            {
                ChildLayout = ChildLayoutMode.Top
            };

            var topBar = new Panel(mainLayer)
            {
                BackgroundPatch = new TexturePatch(0x123456ff),
                ChildLayout = ChildLayoutMode.Left,
                VerticalPadding = 8
            };

            new StyledTextButton(topBar)
            {
                Text = "Save",
                Right = 8,
                OnActivate = Save
            };

            _textEditor = new TextEditor(mainLayer) { Padding = 8, LayoutWeight = 1 };
        }

        public override void OnMounted()
        {
            _textEditor.SetText(File.ReadAllText(FullAssetPath));
        }

        public override void OnUnmounted()
        {
            Save();
        }

        void Save()
        {
            File.WriteAllText(FullAssetPath, _textEditor.GetText());
        }
    }
}
