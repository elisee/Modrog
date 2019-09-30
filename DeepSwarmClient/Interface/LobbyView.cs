using DeepSwarmClient.UI;

namespace DeepSwarmClient.Interface
{
    class LobbyView : InterfaceElement
    {
        public LobbyView(Interface @interface)
            : base(@interface, null)
        {
            const int PanelWidth = 320 + 16 * 2;

            var panel = new Panel(Desktop, this, new Color(0x88aa88ff))
            {
                Anchor = new Anchor(width: PanelWidth),
            };

            var y = 8;

            new Label(Desktop, panel)
            {
                Anchor = new Anchor(left: 8, top: y),
                Text = "Lobby"
            };

            y += 16 + 8;

            // TODO: Display player list
            // TODO: Display saved games & scenarios to choose from
            // TODO: Add buttons for readying up and starting the game

            panel.Anchor.Height = y;
        }
    }
}
