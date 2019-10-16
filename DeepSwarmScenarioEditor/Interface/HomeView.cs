using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.Interface;
using DeepSwarmPlatform.UI;

namespace DeepSwarmScenarioEditor.Interface
{
    class HomeView : InterfaceElement
    {
        public HomeView(Interface @interface)
            : base(@interface, null)
        {
            var windowPanel = new Panel(this, new TexturePatch(0x228800ff))
            {
                Width = 480,
                Flow = Flow.Shrink,
                ChildLayout = ChildLayoutMode.Top,
            };

            var titlePanel = new Panel(windowPanel, new TexturePatch(0x88aa88ff));
            new Label(titlePanel) { Text = "- DeepSwarm Scenario Editor -", Flow = Flow.Shrink, Padding = 8 };

            var mainPanel = new Panel(windowPanel, new TexturePatch(0x228800ff))
            {
                Padding = 8,
                ChildLayout = ChildLayoutMode.Top,
            };

            new Label(mainPanel) { Text = "Choose a scenario:", Bottom = 8 };

            var actionsContainer = new Element(mainPanel) { ChildLayout = ChildLayoutMode.Left, Top = 16 };

            new StyledTextButton(actionsContainer)
            {
                Text = "Edit",
                OnActivate = Validate
            };
        }

        public override void OnMounted()
        {
            Desktop.SetFocusedElement(this);
        }
    }
}
