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
        public readonly Panel ChildrenPanel;

        public static readonly TexturePatch[] IconsByAssetType = new TexturePatch[] {
            new TexturePatch(0x222222ff), // Unknown
            new TexturePatch(0xaa8800ff), // Folder
            new TexturePatch(0x668888ff), // Manifest
            new TexturePatch(0x2222ffff), // Image
            new TexturePatch(0x22ff22ff), // TileSet
            new TexturePatch(0xff2222ff), // Map
            new TexturePatch(0xffff22ff), // Script
        };

        public AssetTreeItem(Element parent, AssetTree tree, AssetEntry entry) : base(parent.Desktop, parent)
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
                Text = entry.Name,
                Ellipsize = true,
            };

            ChildrenPanel = new Panel(this)
            {
                ChildLayout = ChildLayoutMode.Top,
                Left = 24
            };
        }

        public void ToggleChildren()
        {
            ChildrenPanel.IsVisible = !ChildrenPanel.IsVisible;
            _label.Text = Entry.Name + (ChildrenPanel.IsVisible ? "" : $" ({ChildrenPanel.Children.Count})");
        }
    }
}
