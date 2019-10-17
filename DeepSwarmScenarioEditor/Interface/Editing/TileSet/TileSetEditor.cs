using DeepSwarmPlatform.UI;
using System.IO;

namespace DeepSwarmScenarioEditor.Interface.Editing.TileSet
{
    class TileSetEditor : InterfaceElement
    {
        TextEditor _textEditor;

        public TileSetEditor(Interface @interface, Element parent = null) : base(@interface, null)
        {
            _textEditor = new TextEditor(this) { Padding = 8 };

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
