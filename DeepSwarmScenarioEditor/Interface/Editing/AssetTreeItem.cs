using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.UI;
using DeepSwarmScenarioEditor.Scenario;

namespace DeepSwarmScenarioEditor.Interface.Editing
{
    class AssetTreeItem : Element
    {
        public readonly AssetTree Tree;
        public readonly AssetEntry Entry;

        readonly Element _icon;
        readonly Label _label;
        readonly Panel _childrenPanel;

        public static readonly TexturePatch[] IconsByAssetType = new TexturePatch[] {
            new TexturePatch(0x222222ff), // Unknown
            new TexturePatch(0xaa8800ff), // Folder
            new TexturePatch(0x668888ff), // Manifest
            new TexturePatch(0x2222ffff), // Image
            new TexturePatch(0x22ff22ff), // TileSet
            new TexturePatch(0xff2222ff), // Map
            new TexturePatch(0xffff22ff), // Script
        };

        public AssetTreeItem(AssetTree tree, AssetEntry entry) : base(tree.Desktop, null)
        {
            Tree = tree;
            Entry = entry;
            ChildLayout = ChildLayoutMode.Top;

            var button = new Button(this)
            {
                ChildLayout = ChildLayoutMode.Left,
                OnActivate = () => Tree.Internal_ActivateItem(this),
                Padding = 8,
            };

            _icon = new Element(button)
            {
                Width = 16,
                Height = 16,
                Right = 8,
            };

            _icon.BackgroundPatch = IconsByAssetType[(int)entry.AssetType];

            _label = new Label(button)
            {
                LayoutWeight = 1,
                Ellipsize = true,
            };

            _childrenPanel = new Panel(this)
            {
                ChildLayout = ChildLayoutMode.Top,
                Left = 24,
                IsVisible = false
            };

            UpdateLabel();
        }

        public void AddChildItem(AssetTreeItem item)
        {
            _childrenPanel.Add(item);
            UpdateLabel();
        }

        public void ToggleChildren()
        {
            _childrenPanel.IsVisible = !_childrenPanel.IsVisible;
            UpdateLabel();
        }

        void UpdateLabel()
        {
            _label.Text = Entry.Name + (_childrenPanel.IsVisible || _childrenPanel.Children.Count == 0 ? "" : $" ({_childrenPanel.Children.Count})");
        }
    }
}
