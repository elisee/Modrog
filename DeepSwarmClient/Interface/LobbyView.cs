using DeepSwarmClient.UI;

namespace DeepSwarmClient.Interface
{
    class LobbyView : InterfaceElement
    {
        readonly Panel _playerListPanel;
        readonly Panel _scenarioListPanel;
        readonly Panel _savedGamesListPanel;

        public LobbyView(Interface @interface)
            : base(@interface, null)
        {
            var panel = new Panel(Desktop, this, new TexturePatch(0x88aa88ff))
            {
                ChildLayout = ChildLayoutMode.Top,
            };

            new Label(Desktop, panel) { Text = "Lobby", Padding = 8 };
            var playerListAndMainPanelContainer = new Panel(Desktop, panel, null)
            {
                LayoutWeight = 1,
                ChildLayout = ChildLayoutMode.Left
            };

            {
                var mainPanel = new Panel(Desktop, playerListAndMainPanelContainer, new TexturePatch(0x228800ff))
                {
                    LayoutWeight = 1,
                    ChildLayout = ChildLayoutMode.Left
                };

                {
                    var gameSelectionPanel = new Panel(Desktop, mainPanel, null)
                    {
                        Width = 260,
                        ChildLayout = ChildLayoutMode.Top,
                    };

                    new Label(Desktop, gameSelectionPanel)
                    {
                        Padding = 8,
                        Text = "Scenarios",
                        BackgroundPatch = new TexturePatch(0x112345ff),
                    };

                    _scenarioListPanel = new Panel(Desktop, gameSelectionPanel, null)
                    {
                        LayoutWeight = 1,
                        ChildLayout = ChildLayoutMode.Top,
                        VerticalFlow = Flow.Scroll
                    };

                    new Label(Desktop, gameSelectionPanel)
                    {
                        Padding = 8,
                        Text = "Saved Games",
                        BackgroundPatch = new TexturePatch(0x112345ff),
                    };

                    _savedGamesListPanel = new Panel(Desktop, gameSelectionPanel, null)
                    {
                        LayoutWeight = 1,
                        ChildLayout = ChildLayoutMode.Top,
                        VerticalFlow = Flow.Scroll
                    };
                }

                {
                    var gameInfoPanel = new Panel(Desktop, mainPanel, new TexturePatch(0x456721ff))
                    {
                        LayoutWeight = 1,
                        ChildLayout = ChildLayoutMode.Top,
                    };

                    // TODO: Min / Max players, Description, and populate the list of existing players
                }
            }

            {
                // TODO: Move this in the lower part of the game info panel and separate players between known identities & unknown if loading a saved game
                // TODO: Add chat box
                var playerListArea = new Panel(Desktop, playerListAndMainPanelContainer, new TexturePatch(0xaa0000ff))
                {
                    Width = 200,
                    ChildLayout = ChildLayoutMode.Top,
                };

                new Label(Desktop, playerListArea) { Text = "Player list", Padding = 8, BackgroundPatch = new TexturePatch(0x112345ff) };

                _playerListPanel = new Panel(Desktop, playerListArea, null)
                {
                    LayoutWeight = 1,
                    ChildLayout = ChildLayoutMode.Top
                };
            }

            {
                var actionsContainer = new Panel(Desktop, panel, null)
                {
                    Padding = 8,
                    ChildLayout = ChildLayoutMode.Left
                };

                new TextButton(Desktop, actionsContainer)
                {
                    Text = "Ready",
                    Padding = 8,
                    Right = 8,
                    Flow = Flow.Shrink,
                    BackgroundPatch = new TexturePatch(0x4444aaff),
                    OnActivate = Validate
                };

                new TextButton(Desktop, actionsContainer)
                {
                    Text = "Start Game",
                    Padding = 8,
                    Right = 8,
                    Flow = Flow.Shrink,
                    BackgroundPatch = new TexturePatch(0x4444aaff),
                };

                new TextButton(Desktop, actionsContainer)
                {
                    Text = "Leave",
                    Padding = 8,
                    Right = 8,
                    Flow = Flow.Shrink,
                    BackgroundPatch = new TexturePatch(0x4444aaff),
                    OnActivate = Engine.State.Disconnect
                };
            }

            // TODO: Display saved games & scenarios to choose from
            // TODO: Add buttons for readying up and starting the game
        }

        public override void OnMounted()
        {
            Desktop.SetFocusedElement(this);

            OnPlayerListUpdated();

            _scenarioListPanel.Clear();
            foreach (var entry in Engine.State.ScenarioEntries)
            {
                new TextButton(Desktop, _scenarioListPanel)
                {
                    Padding = 8,
                    Text = entry.Name,
                    OnActivate = () => { /* TODO */ }
                };
            }
            _scenarioListPanel.Layout();

            _savedGamesListPanel.Clear();
            foreach (var entry in Engine.State.SavedGameEntries)
            {
                // TODO: Add date last played and stuff like that
                new TextButton(Desktop, _scenarioListPanel)
                {
                    Padding = 8,
                    Text = entry.ScenarioName,
                    OnActivate = () => { /* TODO */ }
                };
            }
        }

        public void OnPlayerListUpdated()
        {
            _playerListPanel.Clear();

            foreach (var playerEntry in Engine.State.PlayerList)
            {
                var playerPanel = new Panel(Desktop, _playerListPanel, null) { Padding = 8 };
                var label = new Label(Desktop, playerPanel) { Text = $"[{(playerEntry.IsReady ? "x" : " ")}] {playerEntry.Name}" };
            }

            _playerListPanel.Layout();
        }
    }
}
