using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.Interface;
using DeepSwarmPlatform.UI;

namespace DeepSwarmScenarioEditor.Interface.Editing.Map
{
    class MapSettingsLayer : Panel
    {
        readonly MapEditor _mapEditor;
        readonly TextInput _tilesetInput;

        /*
        readonly TextInput _mapWidthInput;
        readonly TextInput _mapHeightInput;
        */

        public MapSettingsLayer(MapEditor mapEditor) : base(mapEditor)
        {
            _mapEditor = mapEditor;

            BackgroundPatch = new TexturePatch(0x00000088);

            var windowPanel = new Panel(this)
            {
                BackgroundPatch = new TexturePatch(0x228800ff),
                Width = 480,
                Flow = Flow.Shrink,
                ChildLayout = ChildLayoutMode.Top
            };

            var titlePanel = new Panel(windowPanel, new TexturePatch(0x88aa88ff));
            new Label(titlePanel) { Text = "- Map settings -", Flow = Flow.Shrink, Padding = 8 };

            var mainPanel = new Panel(windowPanel, new TexturePatch(0x228800ff))
            {
                Padding = 8,
                ChildLayout = ChildLayoutMode.Top,
            };

            new Label(mainPanel) { Text = "Tileset:", Bottom = 8 };
            _tilesetInput = new TextInput(mainPanel)
            {
                Bottom = 8,
                Padding = 8,
                BackgroundPatch = new TexturePatch(0x004400ff),
            };

            // Can we avoid having a fixed map size at all and just auto-extend with chunks dynamically? That would be dope

            /*
            {
                var mapSizeContainer = new Panel(mainPanel) { ChildLayout = ChildLayoutMode.Left, Bottom = 8 };

                new Label(mapSizeContainer) { Text = "Width:", VerticalFlow = Flow.Shrink, RightPadding = 8 };

                _mapWidthInput = new TextInput(mapSizeContainer)
                {
                    Padding = 8,
                    LayoutWeight = 1,
                    BackgroundPatch = new TexturePatch(0x004400ff)
                };

                new Label(mapSizeContainer) { Text = "Height:", VerticalFlow = Flow.Shrink, HorizontalPadding = 8 };

                _mapHeightInput = new TextInput(mapSizeContainer)
                {
                    Padding = 8,
                    LayoutWeight = 1,
                    BackgroundPatch = new TexturePatch(0x004400ff)
                };
            }
            */

            var actionsContainer = new Element(mainPanel) { ChildLayout = ChildLayoutMode.Left, Top = 16 };

            new StyledTextButton(actionsContainer)
            {
                Text = "Apply",
                OnActivate = Validate
            };
        }

        public override void OnMounted()
        {
            _tilesetInput.SetValue(_mapEditor.TileSetPath);
        }

        public override void Validate()
        {
            _mapEditor.TileSetPath = _tilesetInput.Value.Trim();

            IsVisible = false;
        }
    }
}
