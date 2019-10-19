using SwarmPlatform.Graphics;
using SwarmPlatform.UI;

namespace ModrogEditor.Interface.Editing.Map
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
