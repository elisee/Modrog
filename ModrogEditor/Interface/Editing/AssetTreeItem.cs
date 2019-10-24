using ModrogEditor.Scenario;
using SwarmPlatform.Graphics;
using SwarmPlatform.UI;
using System.Collections.Generic;

namespace ModrogEditor.Interface.Editing
{
    class AssetTreeItem : Element
    {
        static readonly TexturePatch SelectedBackgroundColor = new TexturePatch(0x0000ffff);

        public readonly AssetTree Tree;
        public readonly AssetEntry Entry;

        readonly Button _button;
        readonly Element _icon;
        readonly Label _label;
        readonly Panel _childrenPanel;

        public List<Element> ChildrenItem => _childrenPanel.Children;

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

            _button = new Button(this)
            {
                ChildLayout = ChildLayoutMode.Left,
                OnActivate = () => Tree.Internal_ActivateItem(this),
                Padding = 8
            };

            _icon = new Element(_button)
            {
                Width = 16,
                Height = 16,
                Right = 8,
            };

            _icon.BackgroundPatch = IconsByAssetType[(int)entry.AssetType];

            _label = new Label(_button)
            {
                LayoutWeight = 1,
                Ellipsize = true,
            };

            _childrenPanel = new Panel(this)
            {
                ChildLayout = ChildLayoutMode.Top,
                Left = 24,
                Visible = false
            };

            UpdateLabel();
        }

        public string GetText() => _label.Text;

        public void SetSelected(bool selected)
        {
            _button.BackgroundPatch = selected ? SelectedBackgroundColor : null;
        }

        public void AddChildItem(AssetTreeItem item)
        {
            _childrenPanel.Add(item);

            UpdateLabel();
        }

        public void ToggleChildren(bool forceVisible = false)
        {
            _childrenPanel.Visible = forceVisible || !_childrenPanel.Visible;
            UpdateLabel();
        }

        void UpdateLabel()
        {
            _label.Text = Entry.Name + (_childrenPanel.Visible || _childrenPanel.Children.Count == 0 ? "" : $" ({_childrenPanel.Children.Count})");
        }
    }
}
