﻿using SwarmPlatform.UI;
using System;
using System.IO;

namespace ModrogEditor.Interface.Editing.Script
{
    class ScriptEditor : BaseAssetEditor
    {
        public static void CreateEmptyFile(string fullAssetPath)
        {
            File.WriteAllText(fullAssetPath, "");
        }

        readonly TextEditor _textEditor;

        public ScriptEditor(EditorApp @interface, string fullAssetPath)
            : base(@interface, fullAssetPath)
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
                error = "Error while loading script: " + exception.Message;
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
                error = "Error while saving script: " + exception.Message;
                return false;
            }

            error = null;
            return true;
        }
    }
}
