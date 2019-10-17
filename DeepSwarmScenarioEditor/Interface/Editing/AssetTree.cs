using DeepSwarmPlatform.UI;
using DeepSwarmScenarioEditor.Scenario;
using System;

namespace DeepSwarmScenarioEditor.Interface.Editing
{
    class AssetTree : Element
    {
        public Action<AssetEntry> OnActivate;

        public AssetTree(Element parent) : this(parent.Desktop, parent) { }

        public AssetTree(Desktop desktop, Element parent = null)
            : base(desktop, parent)
        {
            ChildLayout = ChildLayoutMode.Top;
        }

        internal void Internal_ActivateItem(AssetTreeItem item)
        {
            if (item.Entry.AssetType == AssetType.Folder)
            {
                item.ToggleChildren();
                Layout();
                return;
            }

            OnActivate(item.Entry);
        }
    }
}
