using ModrogEditor.Scenario;
using SwarmPlatform.Graphics;
using SwarmPlatform.Interface;
using SwarmPlatform.UI;

namespace ModrogEditor.Interface.Editing
{
    class DeleteAssetLayer : Panel
    {
        readonly EditingView _editingView;

        AssetEntry _selectedEntry;

        public DeleteAssetLayer(EditingView editingView) : base(editingView)
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
            new Label(titlePanel) { Text = "- Delete asset -", Flow = Flow.Shrink, Padding = 8 };

            var mainPanel = new Panel(windowPanel, new TexturePatch(0x228800ff))
            {
                Padding = 8,
                ChildLayout = ChildLayoutMode.Top,
            };

            new Label(mainPanel) { Text = "Are you sure you want to delete the selected asset?" };

            var actionsContainer = new Element(mainPanel) { ChildLayout = ChildLayoutMode.Left, Top = 16 };

            new StyledTextButton(actionsContainer)
            {
                Text = "Delete",
                Right = 8,
                OnActivate = Validate
            };

            new StyledTextButton(actionsContainer)
            {
                Text = "Cancel",
                OnActivate = Dismiss
            };
        }

        public void SetSelectedEntry(AssetEntry entry)
        {
            _selectedEntry = entry;
        }

        public override void Validate()
        {
            _editingView.App.State.DeleteAsset(_selectedEntry);
            Visible = false;
        }

        public override void Dismiss()
        {
            Visible = false;
        }
    }
}
