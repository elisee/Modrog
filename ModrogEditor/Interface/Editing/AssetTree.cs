using ModrogEditor.Scenario;
using SwarmPlatform.UI;
using System;

namespace ModrogEditor.Interface.Editing
{
    class AssetTree : Element
    {
        public Action<AssetEntry> OnActivate;

        AssetTreeItem _selectedItem;

        public AssetTree(Element parent) : this(parent.Desktop, parent) { }

        public AssetTree(Desktop desktop, Element parent = null)
            : base(desktop, parent)
        {
            ChildLayout = ChildLayoutMode.Top;
        }

        public void SetSelectedEntry(AssetEntry entry)
        {
            if (_selectedItem != null)
            {
                _selectedItem.SetSelected(false);
                _selectedItem = null;
            }

            if (entry != null && _itemsByEntry.ContainsKey(entry))
            {
                _selectedItem = _itemsByEntry[entry];
                _selectedItem.SetSelected(true);
            }
        }

        internal void Internal_ActivateItem(AssetTreeItem item)
        {
            if (item.Entry.AssetType == AssetType.Folder)
            {
                item.ToggleChildren();
                Layout();
            }

            OnActivate(item.Entry);
        }
    }
}
