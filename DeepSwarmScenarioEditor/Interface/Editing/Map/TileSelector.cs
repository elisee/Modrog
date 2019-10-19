using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.UI;

namespace DeepSwarmScenarioEditor.Interface.Editing.Map
{
    class TileSelector : Panel
    {
        readonly MapEditor _mapEditor;

        public TileSelector(MapEditor mapEditor, Element parent) : base(mapEditor.Desktop, parent)
        {
            _mapEditor = mapEditor;
            BackgroundPatch = new TexturePatch(0x000000ff);
        }
    }
}
