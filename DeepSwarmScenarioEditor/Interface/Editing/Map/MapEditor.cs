using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.Interface;
using DeepSwarmPlatform.UI;

namespace DeepSwarmScenarioEditor.Interface.Editing.Map
{
    class MapEditor : InterfaceElement
    {
        readonly MapSettingsLayer _mapSettingsLayer;

        public MapEditor(Interface @interface, Element parent)
            : base(@interface, null)
        {
            var mainLayer = new Panel(this)
            {
                ChildLayout = ChildLayoutMode.Top
            };

            var topBar = new Panel(mainLayer)
            {
                BackgroundPatch = new TexturePatch(0x123456ff),
                ChildLayout = ChildLayoutMode.Left,
                VerticalPadding = 8
            };

            new StyledTextButton(topBar)
            {
                Text = "Save",
                Right = 8,
                OnActivate = () =>
                {
                    // File.WriteAllBytes
                }
            };

            new StyledTextButton(topBar)
            {
                Text = "Settings",
                OnActivate = () =>
                {
                    _mapSettingsLayer.IsVisible = true;
                    _mapSettingsLayer.Layout(_contentRectangle);
                    Desktop.SetFocusedElement(_mapSettingsLayer);
                }
            };

            var viewport = new Panel(mainLayer)
            {
                LayoutWeight = 1
            };

            _mapSettingsLayer = new MapSettingsLayer(this) { IsVisible = false };

            parent?.Add(this);
        }

        public override void OnMounted()
        {
            var assetEntry = Engine.State.ActiveAssetEntry;
        }
    }
}
