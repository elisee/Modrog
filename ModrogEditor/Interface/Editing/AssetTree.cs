using ModrogEditor.Scenario;
using SwarmPlatform.UI;
using System;
using System.Collections.Generic;

namespace ModrogEditor.Interface.Editing
{
    class AssetTree : Element
    {
        public Action<AssetEntry> OnActivate;

        readonly Dictionary<AssetEntry, AssetTreeItem> _itemsByEntry = new Dictionary<AssetEntry, AssetTreeItem>();

        AssetTreeItem _selectedItem;

        public AssetTree(Element parent) : this(parent.Desktop, parent) { }

        public AssetTree(Desktop desktop, Element parent = null)
            : base(desktop, parent)
        {
            ChildLayout = ChildLayoutMode.Top;
        }

        public void AddEntry(AssetEntry entry)
        {
            var item = _itemsByEntry[entry] = new AssetTreeItem(this, entry);

            if (_itemsByEntry.TryGetValue(entry.Parent, out var parentItem))
            {
                parentItem.AddChildItem(item);
                SortChildrenItem(parentItem.ChildrenItem);
            }
            else
            {
                Add(item);
                SortChildrenItem(Children);
            }
        }

        public void ShowEntry(AssetEntry entry)
        {
            void ShowParent(AssetEntry entry)
            {
                var parentEntry = entry.Parent;
                if (!_itemsByEntry.TryGetValue(entry, out var parentItem)) return;

                parentItem.ToggleChildren(forceVisible: true);

                ShowParent(parentEntry);
            }

            ShowParent(entry);
        }

        void SortChildrenItem(List<Element> children)
        {
            var folderItems = new List<Element>();
            var otherItems = new List<Element>();

            foreach (var child in children)
            {
                var assetType = ((AssetTreeItem)child).Entry.AssetType;
                if (assetType == AssetType.Folder) folderItems.Add(child);
                else otherItems.Add(child);
            }

            children.Clear();

            folderItems.Sort((a, b) => string.Compare(((AssetTreeItem)a).GetText(), ((AssetTreeItem)b).GetText()));
            foreach (var item in folderItems) children.Add(item);

            otherItems.Sort((a, b) => string.Compare(((AssetTreeItem)a).GetText(), ((AssetTreeItem)b).GetText()));
            foreach (var item in otherItems) children.Add(item);
        }

        public void SetSelectedEntry(AssetEntry entry)
        {
            if (_selectedItem != null)
            {
                _selectedItem.SetSelected(false);
                _selectedItem = null;
            }

            if (entry != null)
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
