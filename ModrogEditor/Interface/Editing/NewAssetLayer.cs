using SwarmPlatform.Graphics;
using SwarmPlatform.Interface;
using SwarmPlatform.UI;

namespace ModrogEditor.Interface.Editing
{
    class NewAssetLayer : Panel
    {
        readonly EditingView _editingView;

        readonly TextInput _newAssetInput;
        readonly Label _errorLabel;

        public NewAssetLayer(EditingView editingView) : base(editingView)
        {
            _editingView = editingView;

            BackgroundPatch = new TexturePatch(0x00000088);

            var windowPanel = new Panel(this)
            {
                BackgroundPatch = new TexturePatch(0x228800ff),
                Width = 480,
                Flow = Flow.Shrink,
                ChildLayout = ChildLayoutMode.Top
            };

            var titlePanel = new Panel(windowPanel, new TexturePatch(0x88aa88ff));
            new Label(titlePanel) { Text = "- New asset -", Flow = Flow.Shrink, Padding = 8 };

            var mainPanel = new Panel(windowPanel, new TexturePatch(0x228800ff))
            {
                Padding = 8,
                ChildLayout = ChildLayoutMode.Top,
            };

            new Label(mainPanel) { Text = "Name:", Bottom = 8 };
            _newAssetInput = new TextInput(mainPanel)
            {
                Bottom = 8,
                Padding = 8,
                BackgroundPatch = new TexturePatch(0x004400ff),
                OnChange = () =>
                {
                    _errorLabel.Visible = false;
                    Layout();
                }
            };

            _errorLabel = new Label(mainPanel) { Text = "ERROR", Padding = 8, BackgroundPatch = new TexturePatch(0xff0000ff), Visible = false };

            var actionsContainer = new Element(mainPanel) { ChildLayout = ChildLayoutMode.Left, Top = 16 };

            new StyledTextButton(actionsContainer)
            {
                Text = "Apply",
                OnActivate = Validate
            };
        }

        public override void OnMounted()
        {
            _newAssetInput.SetValue("New asset name");
            _newAssetInput.SelectAll();

            Desktop.SetFocusedElement(_newAssetInput);
        }

        public override void Validate()
        {
            var newAssetName = _newAssetInput.Value.Trim();
            var parentEntry = _editingView.GetSelectedAssetTreeFolderEntry();

            if (!_editingView.App.State.TryCreateAsset(newAssetName, parentEntry, out var assetEntry, out var error))
            {
                _errorLabel.Text = error;
                _errorLabel.Visible = true;
                Layout();
            }
            else
            {
                Visible = false;

                if (assetEntry.AssetType != Scenario.AssetType.Folder) _editingView.OpenOrFocusEditor(assetEntry);
            }
        }

        public override void Dismiss()
        {
            Visible = false;
        }
    }
}
