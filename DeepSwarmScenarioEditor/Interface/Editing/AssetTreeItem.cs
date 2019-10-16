using DeepSwarmPlatform.UI;
using DeepSwarmScenarioEditor.Scenario;

namespace DeepSwarmScenarioEditor.Interface.Editing
{
    class AssetTreeItem : Element
    {
        public readonly AssetTree Tree;

        public readonly Panel ChildrenPanel;
        readonly TextButton _button;

        public AssetTreeItem(Element parent, AssetTree tree, AssetEntry entry) : base(parent.Desktop, parent)
        {
            Tree = tree;
            ChildLayout = ChildLayoutMode.Top;

            _button = new TextButton(this)
            {
                Text = entry.Name,
                Padding = 8,
                OnActivate = () => Tree.OnActivate(entry)
            };

            ChildrenPanel = new Panel(this)
            {
                ChildLayout = ChildLayoutMode.Top,
                Left = 24
            };
        }
    }
}
