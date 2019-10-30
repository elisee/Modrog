using SwarmPlatform.Graphics;
using SwarmPlatform.Interface;
using SwarmPlatform.UI;
using System;
using static ModrogEditor.Interface.Editing.EditingView;

namespace ModrogEditor.Interface.Editing
{
    class ConfirmCloseLayer : Panel
    {
        readonly EditingView _editingView;

        readonly Label _errorLabel;
        readonly Label _detailsLabel;

        BaseEditor _editor;
        Action _onCloseEditor;

        public ConfirmCloseLayer(EditingView editingView) : base(editingView)
        {
            _editingView = editingView;

            BackgroundPatch = new TexturePatch(0x00000088);
            Visible = false;

            var windowPanel = new Panel(this)
            {
                BackgroundPatch = new TexturePatch(0x228800ff),
                Width = 480,
                Flow = Flow.Shrink,
                ChildLayout = ChildLayoutMode.Top,
            };

            var titlePanel = new Panel(windowPanel, new TexturePatch(0x88aa88ff));
            new Label(titlePanel) { Flow = Flow.Shrink, Padding = 8, Text = "Save before closing" };
            _detailsLabel = new Label(windowPanel) { Wrap = true, Padding = 8 };

            _errorLabel = new Label(windowPanel) { Text = "", Padding = 8, BackgroundPatch = new TexturePatch(0xff0000ff), Visible = false };

            var buttonsContainer = new Element(windowPanel) { Top = 8, ChildLayout = ChildLayoutMode.Left, Padding = 8 };

            new StyledTextButton(buttonsContainer)
            {
                Right = 8,
                Text = "Save",
                OnActivate = () =>
                {
                    if (!_editor.TrySave(out var error))
                    {
                        _errorLabel.Text = error;
                        _errorLabel.Visible = true;
                        Layout();
                        return;
                    }

                    Close(wasEditorClosed: true);
                }
            };

            new StyledTextButton(buttonsContainer) { Right = 8, Text = "Discard changes", OnActivate = () => { Close(wasEditorClosed: true); } };
            new StyledTextButton(buttonsContainer) { Text = "Don't close", OnActivate = () => Close(wasEditorClosed: false) };

            void Close(bool wasEditorClosed)
            {
                _editor = null;
                Visible = false;
                Desktop.SetFocusedElement(_editingView);

                var callback = _onCloseEditor;
                _onCloseEditor = null;
                if (wasEditorClosed) callback?.Invoke();

            }
        }

        public void Open(EditorUI editorUI, Action onClose)
        {
            _editor = editorUI.Editor;
            _onCloseEditor = onClose;

            _detailsLabel.Text = $"Save changes to {editorUI.Tab.Entry.Path} before closing?";

            _errorLabel.Text = "";
            _errorLabel.Visible = false;

            Visible = true;
        }
    }
}
