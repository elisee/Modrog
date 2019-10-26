using SwarmPlatform.UI;
using System;
using System.IO;

namespace ModrogEditor.Interface.Editing.TileSet
{
    class TileSetEditor : BaseEditor
    {
        public static void CreateEmptyFile(string fullAssetPath)
        {
            File.WriteAllText(fullAssetPath, "{}");
        }

        readonly TextEditor _textEditor;

        public TileSetEditor(EditorApp @interface, string fullAssetPath, EditorTabButton tab)
            : base(@interface, fullAssetPath, tab)
        {
            _textEditor = new TextEditor(_mainLayer)
            {
                Padding = 8,
                LayoutWeight = 1,
                OnChange = MarkUnsavedChanges
            };

            Load();
        }

        public override void OnMounted()
        {
            Desktop.SetFocusedElement(_textEditor);
        }

        protected override bool TryLoad(out string error)
        {
            try
            {
                _textEditor.SetText(File.ReadAllText(FullAssetPath));
            }
            catch (Exception exception)
            {
                error = "Error while loading tile set: " + exception.Message;
                return false;
            }

            error = null;
            return true;
        }

        protected override void Unload()
        {
            // Nothing
        }


        protected override bool TrySave(out string error)
        {
            try
            {
                File.WriteAllText(FullAssetPath, _textEditor.GetText());
            }
            catch (Exception exception)
            {
                error = "Error while saving tile set: " + exception.Message;
                return false;
            }

            error = null;
            return true;
        }
    }
}
