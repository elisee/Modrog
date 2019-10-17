using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.Interface;
using DeepSwarmPlatform.UI;
using System.IO;

namespace DeepSwarmScenarioEditor.Interface.Editing.Script
{
    class ScriptEditor : InterfaceElement
    {
        TextEditor _textEditor;

        public ScriptEditor(Interface @interface, Element parent)
            : base(@interface, null)
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
                OnActivate = () =>
                {
                    File.WriteAllText(Engine.State.GetActiveAssetFullPath(), _textEditor.GetText());
                }
            };

            _textEditor = new TextEditor(mainLayer) { Padding = 8, LayoutWeight = 1 };

            parent?.Add(this);
        }

        public override void OnMounted()
        {
            _textEditor.SetText(File.ReadAllText(Engine.State.GetActiveAssetFullPath()));
        }

        public override void OnUnmounted()
        {
        }
    }
}
